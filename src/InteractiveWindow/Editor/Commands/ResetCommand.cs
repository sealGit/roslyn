using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace Roslyn.Editor.InteractiveWindow.Commands
{
    [Export(typeof(IInteractiveWindowCommand))]
    internal sealed class ResetCommand : InteractiveWindowCommand
    {
        private const string CommandName = "reset";
        private const string NoConfigParameterName = "noconfig";
        private readonly IStandardClassificationService registry;

        [ImportingConstructor]
        public ResetCommand(IStandardClassificationService registry)
        {
            this.registry = registry;
        }

        public override string Description
        {
            get { return "Reset the execution environment to the initial state, keep REPL history."; }
        }

        public override string Name
        {
            get { return CommandName; }
        }

        public override string CommandLine
        {
            get { return "[" + NoConfigParameterName + "]"; }
        }

        public override IEnumerable<KeyValuePair<string, string>> ParametersDescription
        {
            get
            {
                return new ReadOnlyCollection<KeyValuePair<string, string>>(new[]
                {
                    new KeyValuePair<string, string>(NoConfigParameterName, "Reset to a clean environment (only mscorlib referenced), do not run initialization script.")
                });
            }
        }

        public override Task<ExecutionResult> Execute(IInteractiveWindow window, string arguments)
        {
            int noConfigStart, noConfigEnd;
            bool? init = ParseArguments(arguments, out noConfigStart, out noConfigEnd);
            if (init == null)
            {
                ReportInvalidArguments(window);
                return ExecutionResult.Failed;
            }

            return ((InteractiveWindow)window).ResetAsync(init.Value);
        }

        internal static string BuildCommandLine(bool initialize)
        {
            string result = CommandName;
            return initialize ? result : result + " " + NoConfigParameterName;
        }

        public override IEnumerable<ClassificationSpan> ClassifyArguments(ITextSnapshot snapshot, Span argumentsSpan, Span spanToClassify)
        {
            string arguments = snapshot.GetText(argumentsSpan);

            int noConfigStart, noConfigEnd;
            bool? init = ParseArguments(arguments, out noConfigStart, out noConfigEnd);

            if (noConfigStart >= 0)
            {
                yield return new ClassificationSpan(new SnapshotSpan(snapshot, Span.FromBounds(argumentsSpan.Start + noConfigStart, argumentsSpan.Start + noConfigEnd)), registry.Keyword);
            }
        }

        private static bool? ParseArguments(string arguments, out int noConfigStart, out int noConfigEnd)
        {
            noConfigStart = noConfigEnd = -1;

            string noconfig = arguments.Trim();
            if (noconfig.Length == 0)
            {
                return true;
            }

            if (string.Compare(noconfig, NoConfigParameterName, StringComparison.OrdinalIgnoreCase) == 0)
            {
                noConfigStart = arguments.IndexOf(noconfig, StringComparison.OrdinalIgnoreCase);
                noConfigEnd = noConfigStart + noconfig.Length;
                return false;
            }

            return null;
        }
    }
}
