﻿using System;
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
    [ContentType(NSConst.LangName)]
    internal class NSSigSourceProvider: ISignatureHelpSourceProvider {
        [Import] internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }
        [Import] internal ITextBufferFactoryService TextBufferFactoryService { get; set; }

        public ISignatureHelpSource TryCreateSignatureHelpSource(ITextBuffer textBuffer) {
            return new NSSigSource(this, textBuffer);
        }
    }

    internal class NSSigSource: ISignatureHelpSource {

        internal ITextBuffer m_textBuffer;

        public NSSigSource(ITextBuffer textBuffer) {
            m_textBuffer = textBuffer;
        }

        sealed class NSSigParameter: IParameter {
            readonly Span m_trackingspan;
            readonly string m_content;
            readonly string m_prettyprintedcontent;
            readonly string m_documentation;

            //string documentation, Span locus, string name, ISignature signature
            internal NSSigParameter(Span trackingspan, string content, string prettyprintedcontent, string documentation) {
                m_trackingspan = trackingspan;
                m_content = content;
                m_prettyprintedcontent = prettyprintedcontent;
                m_documentation = documentation;
            }
        }

        sealed class NSSignature: ISignature {

            internal ITextBuffer m_subjectBuffer;
            internal IParameter m_currentParameter;
            internal string m_content;
            internal string m_documentation;
            internal ITrackingSpan m_applicableToSpan;
            internal ReadOnlyCollection<IParameter> m_parameters;
            internal string m_printContent;
            internal event EventHandler<CurrentParameterChangedEventArgs> CurrentParameterChanged;

            internal NSSignature(ITextBuffer subjectBuffer, string content, string doc, ReadOnlyCollection<IParameter> parameters) {
                m_subjectBuffer = subjectBuffer;
                m_content = content;
                m_documentation = doc;
                m_parameters = parameters;
                m_subjectBuffer.Changed += new EventHandler<TextContentChangedEventArgs>(OnSubjectBufferChanged);
            }

            public IParameter CurrentParameter {
                get { return m_currentParameter; }
                internal set {
                    if (m_currentParameter != value) {
                        IParameter prevCurrentParameter = m_currentParameter;
                        m_currentParameter = value;
                        this.RaiseCurrentParameterChanged(prevCurrentParameter, m_currentParameter);
                    }
                }
            }

            private void RaiseCurrentParameterChanged(IParameter prevCurrentParameter, IParameter newCurrentParameter) {
                EventHandler<CurrentParameterChangedEventArgs> tempHandler = this.CurrentParameterChanged;
                if (tempHandler != null) {
                    tempHandler(this, new CurrentParameterChangedEventArgs(prevCurrentParameter, newCurrentParameter));
                }
            }

            internal void OnSubjectBufferChanged(object sender, TextContentChangedEventArgs e) {
                this.ComputeCurrentParameter();
            }

            internal void ComputeCurrentParameter() {
                if (m_parameters.Count == 0) {
                    this.CurrentParameter = null;
                    return;
                }

                //the number of commas in the string is the index of the current parameter 
                string sigText = m_applicableToSpan.GetText(m_subjectBuffer.CurrentSnapshot);
                int currentIndex = 0;
                int commaCount = 0;
                while (currentIndex < sigText.Length) {
                    int commaIndex = sigText.IndexOf(',', currentIndex);
                    if (commaIndex == -1) {
                        break;
                    }
                    commaCount++;
                    currentIndex = commaIndex + 1;
                }

                if (commaCount < m_parameters.Count) {
                    this.CurrentParameter = m_parameters[commaCount];
                } else {
                    //too many commas, so use the last parameter as the current one. 
                    this.CurrentParameter = m_parameters[m_parameters.Count - 1];
                }
            }

        }

        public void AugmentSignatureHelpSession(ISignatureHelpSession session, IList<ISignature> signatures) {
            //if (!session.TextView.Properties.ContainsProperty(SessionKey)) {
            //    return;
            //}
            signatures.Clear();

            // Map the trigger point down to our buffer.
            //var subjectTriggerPoint = session.GetTriggerPoint(subjectBuffer.CurrentSnapshot);
            //if (!subjectTriggerPoint.HasValue) {
            //    return;
            //}

            ITextSnapshot snapshot = m_textBuffer.CurrentSnapshot;
            int position = session.GetTriggerPoint(m_textBuffer).GetPosition(snapshot);
            ITrackingSpan applicableToSpan = m_textBuffer.CurrentSnapshot.CreateTrackingSpan(new Span(position, 0), SpanTrackingMode.EdgeInclusive, 0);

            //var currentSnapshot = subjectTriggerPoint.Value.Snapshot;
            //var querySpan = new SnapshotSpan(subjectTriggerPoint.Value, 0);
            //var applicableToSpan = currentSnapshot.CreateTrackingSpan(querySpan.Start.Position, 0, SpanTrackingMode.EdgeInclusive);
            NSUtil.DebugPrintAlways("Sighelp");
            signatures.Add(CreateSignature(m_textBuffer, "add(int firstInt, int secondInt)", "Documentation for adding integers.", applicableToSpan));
            signatures.Add(CreateSignature(m_textBuffer, "add(double firstDouble, double secondDouble)", "Documentation for adding doubles.", applicableToSpan));

            //string sighelp = "hey1";
            //if (sighelp != null) {
            //}
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


        public ISignature GetBestMatch(ISignatureHelpSession session) {
            return session.Signatures.Count > 0 ? session.Signatures[0] : null;
        }

        private NSSignature CreateSignature(ITextBuffer textBuffer, string methodSig, string methodDoc, ITrackingSpan span) {
            NSSignature sig = new NSSignature(textBuffer, methodSig, methodDoc, null);
            textBuffer.Changed += new EventHandler<TextContentChangedEventArgs>(sig.OnSubjectBufferChanged);

            //find the parameters in the method signature (expect methodname(one, two) 
            string[] pars = methodSig.Split(new char[] { '(', ',', ')' });
            List<IParameter> paramList = new List<IParameter>();

            int locusSearchStart = 0;
            for (int i = 1; i < pars.Length; i++) {
                string param = pars[i].Trim();

                if (string.IsNullOrEmpty(param))
                    continue;

                //find where this parameter is located in the method signature 
                int locusStart = methodSig.IndexOf(param, locusSearchStart);
                if (locusStart >= 0) {
                    Span locus = new Span(locusStart, param.Length);
                    locusSearchStart = locusStart + param.Length;
                    paramList.Add(new NSSigParameter(locus, "Documentation for the parameter.", param, sig));
                }
            }

            sig.m_parameters = new ReadOnlyCollection<IParameter>(paramList);
            sig.m_applicableToSpan = span;
            sig.ComputeCurrentParameter();
            return sig;
        }
    
    }

}
