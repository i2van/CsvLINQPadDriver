﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;

using CsvLINQPadDriver.Extensions;
using CsvLINQPadDriver.Wpf.Extensions;

namespace CsvLINQPadDriver;

internal partial class ConnectionDialog
{
    private object? _originalFilesTextBoxToolTip;
    private bool _addFoldersWithSubfoldersDialogOpened;
    private bool _skipConfirmOption;

    private ICsvDataContextDriverProperties TypedDataContext =>
        (ICsvDataContextDriverProperties)DataContext;

    public ConnectionDialog(ICsvDataContextDriverProperties csvDataContextDriverProperties)
    {
        DataContext = csvDataContextDriverProperties;

        OverrideDependencyPropertiesMetadata();
        InitializeComponent();
        UpdateInstructions();
        AddCommandManagerPreviewHandlers();

        static void OverrideDependencyPropertiesMetadata()
        {
            Array.ForEach([typeof(Control), typeof(Hyperlink)], static type => ToolTipService.ShowOnDisabledProperty.OverrideMetadata(type, new FrameworkPropertyMetadata(true)));

            var showDurationPropertyType = typeof(FrameworkElement);
            var showDurationProperty = ToolTipService.ShowDurationProperty;
            var duration = showDurationProperty.GetMetadata(showDurationPropertyType);
            showDurationProperty.OverrideMetadata(showDurationPropertyType, new FrameworkPropertyMetadata((int)duration.DefaultValue * 125 / 100));
        }

        void AddCommandManagerPreviewHandlers()
        {
            CommandManager.AddPreviewExecutedHandler(FilesTextBox, FilesTextBox_OnPreviewExecuted);
            CommandManager.AddPreviewCanExecuteHandler(FilesTextBox, FilesTextBox_OnPreviewCanExecute);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        CommandManager.RemovePreviewExecutedHandler(FilesTextBox, FilesTextBox_OnPreviewExecuted);
        CommandManager.RemovePreviewCanExecuteHandler(FilesTextBox, FilesTextBox_OnPreviewCanExecute);
    }

    private void UpdateInstructions()
    {
        if (!IsInitialized)
        {
            return;
        }

        const string inlineComment = FileExtensions.InlineComment;

        var fileType = TypedDataContext.FileType;
        var mask = fileType.GetMask();
        var recursiveMask = fileType.GetMask(true);

        var wildcardsToolTip = $"Type one file/folder per line. Wildcards ? and * are supported; {recursiveMask} searches in folder and its sub-folders";

        FilesInstructionsTextBox.Text = GetInstructions().Select(static str => $"{inlineComment} {str}.").JoinNewLine();
        FilesTextBox.ToolTip = $"{_originalFilesTextBoxToolTip ??= FilesTextBox.ToolTip} or {char.ToLower(wildcardsToolTip[0])}{wildcardsToolTip[1..]}".Replace(". ", Environment.NewLine);

        IEnumerable<string> GetInstructions()
        {
            yield return "Drag&drop here (from add files/folder dialogs as well). Ctrl adds files. Alt toggles * and ** masks";
            yield return wildcardsToolTip;
            yield return $"{((KeyGesture)ApplicationCommands.Paste.InputGestures[0]).DisplayString} ({PasteFoldersWithSubfoldersCommand.InputGestureText}) pastes from clipboard, appends {mask} ({recursiveMask}) to folders";
            yield return $"{PasteFromClipboardFoldersAndProceedCommand.InputGestureText} ({PasteFromClipboardFoldersWithSubfoldersAndProceedCommand.InputGestureText}) clears, pastes from clipboard, appends {mask} ({recursiveMask}) to folders and proceeds";
            yield return $"Use '{inlineComment}' to comment a line";
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (CanClose())
        {
            DialogResult = true;
        }

        bool CanClose()
        {
            var validateFilePaths = TypedDataContext.ValidateFilePaths;
            if (!validateFilePaths)
            {
                return true;
            }

            var invalidFilePaths = GetInvalidFilePaths();
            if (!invalidFilePaths.Any())
            {
                return true;
            }

            var instructionText = $"Invalid file {invalidFilePaths.Pluralize("path")}";

            var canClose =
                this.ShowYesNoDialog(
                    $"{instructionText} found",
                    $"Would you like to correct {invalidFilePaths.Pluralize("it", "them")}?",
                    "Go back and correct. Only absolute and network paths are supported",
                    "Proceed as is",
                    instructionText.ToLowerInvariant(),
                    invalidFilePaths.JoinNewLine(),
                    ValidateFilePathsCheckBox.ReplaceHotKeyChar("&"),
                    ref validateFilePaths) == false;

            try
            {
                _skipConfirmOption = true;

                TypedDataContext.ValidateFilePaths = validateFilePaths;
                ValidateFilePathsCheckBox.UpdateTargetBinding(ToggleButton.IsCheckedProperty);
            }
            finally
            {
                _skipConfirmOption = false;
            }

            if (!canClose)
            {
                FilesTextBox.Focus();
            }

            return canClose;

            IReadOnlyCollection<string> GetInvalidFilePaths() =>
                FilesTextBox.Text.GetFiles().Where(static file => !IsPathValid(file)).ToList();
        }
    }

#pragma warning disable S2325
    private void FilesTextBox_DragEnter(object sender, DragEventArgs e)
#pragma warning restore S2325
    {
        e.Handled =
            e.Data.GetDataPresent(DataFormats.FileDrop) ||
            e.Data.GetDataPresent(DataFormats.StringFormat);

        e.Effects = e.Handled
            ? IsDragAndDropInAddMode(e.KeyStates)
                ? DragDropEffects.Copy
                : DragDropEffects.Move
            : DragDropEffects.None;
    }

    private void FilesTextBox_DragDrop(object sender, DragEventArgs e)
    {
        if (!IsDragAndDropInAddMode(e.KeyStates))
        {
            FilesTextBox.Clear();
        }

        var enrichedFiles = GetEnrichedPathsFromUserInput(
            (e.Data.GetData(DataFormats.FileDrop, true) ?? e.Data.GetData(DataFormats.StringFormat))!,
            IsDragAndDropInFoldersWithSubfoldersMode() ^ _addFoldersWithSubfoldersDialogOpened);

        AppendFiles(enrichedFiles);

        e.Handled = true;

        bool IsDragAndDropInFoldersWithSubfoldersMode() =>
            e.KeyStates.HasFlag(DragDropKeyStates.AltKey);
    }

    private static bool CanExecutePastePreview(ICommand command) =>
        command == ApplicationCommands.Paste && CanExecutePaste();

    private static void FilesTextBox_OnPreviewCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        if (CanExecutePastePreview(e.Command))
        {
            e.CanExecute = true;
            e.Handled = true;
        }
    }

    private void FilesTextBox_OnPreviewExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (CanExecutePastePreview(e.Command))
        {
            InsertFilesFromClipboard(false);
            e.Handled = true;
        }
    }

    private void PasteFoldersWithSubfoldersCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e) =>
        InsertFilesFromClipboard(true);

    private void InsertFilesFromClipboard(bool foldersWithSubfolders)
    {
        var enrichedFilesText = GetFilesTextFromClipboard(foldersWithSubfolders);

        FilesTextBox.SelectedText = enrichedFilesText;
        FilesTextBox.SelectionLength = 0;
        FilesTextBox.SelectionStart += enrichedFilesText.Length;
    }

    private string[] GetEnrichedPathsFromUserInput(object data, bool foldersWithSubfolders)
    {
        var files = data as IEnumerable<string> ?? ((string) data).GetFiles();

        return files.Select(GetEnrichedPath).ToArray();

        string GetEnrichedPath(string path)
        {
            var (isFile, enrichedPath) = path.DeduceIsFileOrFolder();
            return isFile
                ? enrichedPath
                : Path.Combine(enrichedPath, GetFolderFilesMask(foldersWithSubfolders));
        }
    }

    private static bool IsDragAndDropInAddMode(DragDropKeyStates keyStates) =>
        keyStates.HasFlag(DragDropKeyStates.ControlKey);

    private void ConnectionDialog_OnLoaded(object sender, RoutedEventArgs e) =>
        MoveCaretToEnd();

#pragma warning disable S2325
    private void CommandBinding_OnCanAlwaysExecute(object sender, CanExecuteRoutedEventArgs e) =>
#pragma warning restore S2325
        e.CanExecute = true;

    private void PasteFromClipboardFoldersAndProceedCommandBinding_OnExecuted(object sender, ExecutedRoutedEventArgs e) =>
        PasteAndGo(false);

    private void PasteFromClipboardFoldersWithSubfoldersAndProceedCommandBinding_OnExecuted(object sender, ExecutedRoutedEventArgs e) =>
        PasteAndGo(true);

    private string GetFilesTextFromClipboard(bool foldersWithSubfolders) =>
        GetEnrichedPathsFromUserInput(Clipboard.GetData(DataFormats.FileDrop) ?? Clipboard.GetText(), foldersWithSubfolders)
            .Aggregate(new StringBuilder(), static (result, file) => result.AppendLine(file))
            .ToString();

    private void PasteAndGo(bool foldersWithSubfolders)
    {
        var enrichedFilesText = GetFilesTextFromClipboard(foldersWithSubfolders);

        FilesTextBox.SelectAll();
        FilesTextBox.SelectedText = enrichedFilesText;
        FilesTextBox.SelectionLength = 0;
        MoveCaretToEnd();

        OkButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
    }

#pragma warning disable S2325
    private void ClipboardCommandBinding_OnCanExecute(object sender, CanExecuteRoutedEventArgs e) =>
#pragma warning restore S2325
        e.CanExecute = CanExecutePaste();

    private static bool CanExecutePaste() =>
        Clipboard.ContainsText(TextDataFormat.Text) ||
        Clipboard.ContainsText(TextDataFormat.UnicodeText) ||
        Clipboard.ContainsFileDropList();

#pragma warning disable S2325
    private void HelpCommandBinding_OnExecuted(object sender, ExecutedRoutedEventArgs e) =>
#pragma warning restore S2325
        (e.OriginalSource switch
        {
            Hyperlink hyperlink => hyperlink.NavigateUri.OriginalString,
            _                   => HelpUri
        }).ShellExecute();

    private void AddFilesCommandBinding_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        var fileType = TypedDataContext.FileType;

        if (this.TryOpenFiles("Add Files", FileExtensions.Filter, fileType.GetExtension(), out var files, fileType.GetFilterIndex()))
        {
            AppendFiles(files);
        }
    }

    private void AddFoldersCommandBinding_OnExecuted(object sender, ExecutedRoutedEventArgs e) =>
        AddFolders(false);

    private void AddFoldersWithSubfoldersCommandBinding_OnExecuted(object sender, ExecutedRoutedEventArgs e) =>
        AddFolders(true);

    private void SelectAllCommandBinding_OnExecuted(object sender, ExecutedRoutedEventArgs e) =>
        ApplicationCommands.SelectAll.Execute(null, FilesTextBox);

    private void SelectAllCommandBinding_OnCanExecute(object sender, CanExecuteRoutedEventArgs e) =>
        e.CanExecute = FilesTextBox.SelectionLength < FilesTextBox.Text.Length;

    private void DeleteCommandBinding_OnExecuted(object sender, ExecutedRoutedEventArgs e) =>
        EditingCommands.Delete.Execute(null, FilesTextBox);

    private void DeleteCommandBinding_OnCanExecute(object sender, CanExecuteRoutedEventArgs e) =>
        e.CanExecute = FilesTextBox.SelectionLength != 0 || FilesTextBox.CaretIndex < FilesTextBox.Text.Length;

    private void ClearCommandBinding_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        FilesTextBox.Clear();
        FilesTextBox.Focus();
    }

    private void ClearCommandBinding_OnCanExecute(object sender, CanExecuteRoutedEventArgs e) =>
        e.CanExecute = FilesTextBox?.Text.Any() == true;

    private void WrapFilesTextCommand_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        FilesTextBox.TextWrapping =
            FilesTextBox.TextWrapping == TextWrapping.Wrap
                ? TextWrapping.NoWrap
                : TextWrapping.Wrap;

        ScrollToActiveLine();
    }

    private void CtrlLeftClickCommandBinding_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        SetCaretIndexFromMousePosition();

        BrowseCommand.Execute(null, FilesTextBox);

        void SetCaretIndexFromMousePosition()
        {
            var caretIndex = FilesTextBox.GetCharacterIndexFromPoint(Mouse.GetPosition(FilesTextBox), true);
            if (caretIndex >= 0)
            {
                FilesTextBox.CaretIndex = caretIndex;
            }
        }
    }

    private void BrowseCommandBinding_OnExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (!TryGetFullPathAtLineIncludingInlineComment(out var fullPath))
        {
            this.ShowWarning("Only absolute paths are supported");
            return;
        }

        ScrollToActiveLine();

        var (isFile, path) = fullPath.DeduceIsFileOrFolder(true);
        var shellResult = path.Explore(isFile);

        if (!shellResult)
        {
            this.ShowWarning(
                "Browse failed for".JoinNewLine(
                    string.Empty,
                    path,
                    string.Empty,
                    shellResult));
        }
    }

    private void ScrollToActiveLine()
    {
        FilesTextBox.ScrollToLine(GetActiveLineIndex());

        int GetActiveLineIndex() =>
            FilesTextBox.GetLineIndexFromCharacterIndex(FilesTextBox.CaretIndex);
    }

    private void BrowseCommandBinding_OnCanExecute(object sender, CanExecuteRoutedEventArgs e) =>
        e.CanExecute = TryGetFullPathAtLineIncludingInlineComment(out _);

    private static bool IsPathValid(string path) =>
#if NETCOREAPP
        Path.IsPathFullyQualified(path);
#else
        Path.IsPathRooted(path);
#endif

    private bool TryGetFullPathAtLineIncludingInlineComment(out string fullPath)
    {
        if (FilesTextBox is null)
        {
            fullPath = string.Empty;
            return false;
        }

        return TryGetLineAtCaret(out fullPath) && IsPathValid(fullPath);

        bool TryGetLineAtCaret(out string line) =>
            !string.IsNullOrWhiteSpace(line = FilesTextBox.GetLineTextAtCaretIndex().GetInlineCommentContent().Trim());
    }

    private void AddFolders(bool withSubfolders)
    {
        _addFoldersWithSubfoldersDialogOpened = withSubfolders;

        if (this.TryBrowseForFolders($"Add Folders{(withSubfolders ? " with Sub-folders" : string.Empty)}", out var folders))
        {
            var folderFilesMask = GetFolderFilesMask(withSubfolders);
            AppendFiles(folders.Select(folder => Path.Combine(folder, folderFilesMask)).ToArray());
        }

        _addFoldersWithSubfoldersDialogOpened = false;
    }

    private string GetFolderFilesMask(bool withSubfolders) =>
        TypedDataContext.FileType.GetMask(withSubfolders);

    private void AppendFiles(params string[] files)
    {
        AppendNewLine();

        FilesTextBox.AppendText(files.JoinNewLine());
        FilesTextBox.AppendText(Environment.NewLine);

        MoveCaretToEnd(true);

        void AppendNewLine()
        {
            if (HasFiles() && !AppendFilesRegex().IsMatch(FilesTextBox.Text))
            {
                FilesTextBox.AppendText(Environment.NewLine);
            }

            bool HasFiles() =>
                !string.IsNullOrWhiteSpace(FilesTextBox?.Text);
        }
    }

    private void MoveCaretToEnd(bool scrollToEnd = false)
    {
        FilesTextBox.CaretIndex = FilesTextBox.Text.Length;
        if (scrollToEnd)
        {
            FilesTextBox.ScrollToEnd();
        }
    }

    private void FileTypeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e) =>
        UpdateInstructions();

    private void ConfirmOption_OnChecked(object sender, RoutedEventArgs e) =>
        ConfirmOption(e);

    private void ConfirmOption_OnUnchecked(object sender, RoutedEventArgs e) =>
        ConfirmOption(e, true);

    private void ConfirmOption(RoutedEventArgs e, bool isUnchecked = false)
    {
        if (_skipConfirmOption ||
            !IsLoaded ||
            e.OriginalSource is not CheckBox { Tag: string tag } checkBox ||
            e.RoutedEvent != (tag == ConfirmCheck ? ToggleButton.CheckedEvent : ToggleButton.UncheckedEvent))
        {
            return;
        }

        checkBox.IsChecked =
            isUnchecked ^ this.ShowYesNoDialog(
                checkBox.ReplaceHotKeyChar(),
                $"This option might mangle CSV parsing if {tag}ed. Would you like to {tag} it anyway?",
                $"I understand and want to {tag} it",
                $"Do not {tag} it",
                false)
            ?? isUnchecked;
    }
}
