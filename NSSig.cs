using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace NimStudio.NimStudio {

    [Export(typeof(ISignatureHelpSourceProvider))]
    [Name("ToolTip SignatureHelp Source")]
    [Order(Before = "Default Signature Help Presenter")]
    [Order(After = "JavaScript Signature Help source")]
    [ContentType(NSConst.LangName)]
    internal class NSSigSourceProvider: ISignatureHelpSourceProvider {
        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService {
            get;
            set;
        }

        [Import]
        internal ITextBufferFactoryService TextBufferFactoryService {
            get;
            set;
        }

        public ISignatureHelpSource TryCreateSignatureHelpSource(ITextBuffer textBuffer) {
            return new NSSigSource(this, textBuffer);
        }
    }

    internal class NSSigSource: ISignatureHelpSource {
        sealed class Signature: ISignature {
            readonly ITrackingSpan trackingSpan;
            readonly string content;
            readonly string prettyPrintedContent;
            readonly string documentation;

            public ITrackingSpan ApplicableToSpan {
                get {
                    return trackingSpan;
                }
            }

            public string Content {
                get {
                    return content;
                }
            }

            public IParameter CurrentParameter {
                get {
                    return null;
                }
            }

            public event EventHandler<CurrentParameterChangedEventArgs> CurrentParameterChanged;

            public string Documentation {
                get {
                    return documentation;
                }
            }

            public ReadOnlyCollection<IParameter> Parameters {
                get {
                    return new ReadOnlyCollection<IParameter>(new IParameter[] { });
                }
            }

            public string PrettyPrintedContent {
                get {
                    return prettyPrintedContent;
                }
            }

            internal Signature(ITrackingSpan trackingSpan, string content, string prettyPrintedContent, string documentation) {
                this.trackingSpan = trackingSpan;
                this.content = content;
                this.prettyPrintedContent = prettyPrintedContent;
                this.documentation = documentation;
            }
        }

        /// <summary>
        ///     This object needs to be added as a key to the property bag of an ITextView where
        ///     encouragement should be applied.  This prevents encouragement from being
        ///     introduced in places like signature overload.
        /// </summary>
        //internal static readonly object SessionKey = new object();

        readonly ITextBuffer subjectBuffer;

        public NSSigSource(NSSigSourceProvider provider, ITextBuffer subjectBuffer) {
            this.subjectBuffer = subjectBuffer;
        }

        bool isDisposed;

        public void Dispose() {
            if (!isDisposed) {
                GC.SuppressFinalize(this);
                isDisposed = true;
            }
        }

        public void AugmentSignatureHelpSession(ISignatureHelpSession session, IList<ISignature> signatures) {
            //if (!session.TextView.Properties.ContainsProperty(SessionKey)) {
            //    return;
            //}
            signatures.Clear();

            // Map the trigger point down to our buffer.
            var subjectTriggerPoint = session.GetTriggerPoint(subjectBuffer.CurrentSnapshot);
            if (!subjectTriggerPoint.HasValue) {
                return;
            }

            var currentSnapshot = subjectTriggerPoint.Value.Snapshot;
            var querySpan = new SnapshotSpan(subjectTriggerPoint.Value, 0);
            var applicableToSpan = currentSnapshot.CreateTrackingSpan(querySpan.Start.Position, 0, SpanTrackingMode.EdgeInclusive);

            string sighelp = "hey1";
            if (sighelp != null) {
                var signature = new Signature(applicableToSpan, sighelp, "", "");
                signatures.Add(signature);
            }
        }

        public ISignature GetBestMatch(ISignatureHelpSession session) {
            return session.Signatures.Count > 0 ? session.Signatures[0] : null;
        }
    }

}
