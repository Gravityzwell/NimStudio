using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Utilities;

namespace NimStudio.NimStudio {


    //internal static class VSNContentType {
    //    [Export, Name("nim"), BaseDefinition("text")]
    //    public static ContentTypeDefinition NimContentType = null;

    //    [Export, FileExtension(".nim"), ContentType("nim")]
    //    public static FileExtensionToContentTypeDefinition NimFileType = null;
    //}

    //[Export(typeof(IClassifierProvider))]
    //[ContentType("nim")]
    //[Name("NimSyntaxProvider")]
    //internal sealed class MyLangSyntaxProvider: IClassifierProvider {
    //    [Import]
    //    internal IClassificationTypeRegistryService ClassificationRegistry = null;

    //    public IClassifier GetClassifier(ITextBuffer buffer) {
    //        return buffer.Properties.GetOrCreateSingletonProperty(() => new VSNLangSyntax(ClassificationRegistry, buffer));
    //    }
    //}

    //internal sealed class VSNLangSyntax: IClassifier {
    //    private ITextBuffer buffer;
    //    private IClassificationType identifierType;
    //    private IClassificationType keywordType;

    //    public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

    //    internal VSNLangSyntax(IClassificationTypeRegistryService registry, ITextBuffer buffer) {
    //        this.identifierType = registry.GetClassificationType(PredefinedClassificationTypeNames.Identifier);
    //        this.keywordType = registry.GetClassificationType(PredefinedClassificationTypeNames.Keyword);
    //        this.buffer = buffer;
    //        this.buffer.Changed += OnBufferChanged;
    //    }

    //    public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan snapshotSpan) {
    //        var classifications = new List<ClassificationSpan>();
    //        string text = snapshotSpan.GetText();
    //        var span = new SnapshotSpan(snapshotSpan.Snapshot, snapshotSpan.Start.Position, text.Length);
    //        classifications.Add(new ClassificationSpan(span, keywordType));
    //        return classifications;
    //    }

    //    private void OnBufferChanged(object sender, TextContentChangedEventArgs e) {
    //        foreach (var change in e.Changes)
    //            ClassificationChanged(this, new ClassificationChangedEventArgs(new SnapshotSpan(e.After, change.NewSpan)));
    //    }
    //}

    [Export(typeof(IIntellisenseControllerProvider))]
    [Name("NimStudio Intellisense Controller")]
    [ContentType(NSConst.LangName)]
    internal class NSIntellisenseControllerProvider: IIntellisenseControllerProvider {

        [Import] internal ISignatureHelpBroker SignatureHelpBroker { get; set; }
        [Import] internal ITextDocumentFactoryService TextDocumentFactoryService = null;
        [Import] internal ICompletionBroker _CompletionBroker = null;
        internal System.IServiceProvider _serviceprovider;

        [ImportingConstructor]
        public NSIntellisenseControllerProvider([Import(typeof(SVsServiceProvider))] System.IServiceProvider serviceProvider) {
            _serviceprovider = serviceProvider;
            //PythonService = serviceProvider.GetPythonToolsService();
        }

        public IIntellisenseController TryCreateIntellisenseController(ITextView textView, IList<ITextBuffer> subjectBuffers) {
            ITextDocument textDocument;
            if (!TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out textDocument)) {
                return null;
            }

            if (!textView.Roles.Contains(PredefinedTextViewRoles.Document)) {
                return null;
            }
            return new NSIntellisenseController(textView, textDocument, this, _serviceprovider);
        }
    }

    internal class NSIntellisenseController: IIntellisenseController, IOleCommandTarget {
        readonly ITextView _textview;
        readonly ITextDocument _textdocument;
        readonly NSIntellisenseControllerProvider _isprovider;
        ISignatureHelpSession session;

        public NSIntellisenseController(ITextView textview, ITextDocument textDocument, NSIntellisenseControllerProvider provider, System.IServiceProvider serviceProvider) {
            _textview = textview;
            _textdocument = textDocument;
            _isprovider = provider;

            textDocument.DirtyStateChanged += OnDocumentDirtyStateChanged;
        }

        void OnDocumentDirtyStateChanged(object sender, EventArgs e) {
            //if (!textDocument.IsDirty) {
                TriggerSignatureHelp();
            //}
        }

        void TriggerSignatureHelp() {
            var point = _textview.Caret.Position.BufferPosition;
            var triggerPoint = point.Snapshot.CreateTrackingPoint(point.Position, PointTrackingMode.Positive);
            if (!_isprovider.SignatureHelpBroker.IsSignatureHelpActive(_textview)) {
                //textView.Properties.AddProperty(NSSigSource.SessionKey, null);
                session = _isprovider.SignatureHelpBroker.TriggerSignatureHelp(_textview, triggerPoint, true);
                //textView.Properties.RemoveProperty(NSSigSource.SessionKey);
            }
        }

        public void Detach(ITextView tview) {
            if (_textview == tview) {
                _textdocument.DirtyStateChanged -= OnDocumentDirtyStateChanged;
            }
        }

        public void ConnectSubjectBuffer(ITextBuffer subjectBuffer) {
        }

        public void DisconnectSubjectBuffer(ITextBuffer subjectBuffer) {
        }
    }


}
