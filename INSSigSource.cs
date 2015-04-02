using System;
namespace NimStudio.NimStudio {
    interface INSSigSource {
        void AugmentSignatureHelpSession(Microsoft.VisualStudio.Language.Intellisense.ISignatureHelpSession session, System.Collections.Generic.IList<Microsoft.VisualStudio.Language.Intellisense.ISignature> signatures);
        void Dispose();
        Microsoft.VisualStudio.Language.Intellisense.ISignature GetBestMatch(Microsoft.VisualStudio.Language.Intellisense.ISignatureHelpSession session);
    }
}
