var row = TrimSeparator.Single();

row.Quoted.Should().Be(context.Quoted, Reason());
row.Unquoted.Should().Be(context.Unquoted, Reason());
