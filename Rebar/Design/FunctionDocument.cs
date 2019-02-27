using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using NationalInstruments.ContextualHelp.View;
using NationalInstruments.Controls.Shell;
using NationalInstruments.Core;
using NationalInstruments.Design;
using NationalInstruments.MocCommon.Design;
using NationalInstruments.MocCommon.SourceModel;
using NationalInstruments.Shell;
using NationalInstruments.SourceModel.Envoys;
using NationalInstruments.VI.Design;
using Rebar.SourceModel;

namespace Rebar.Design
{
    /// <summary>
    /// Rebar function document type
    /// </summary>
    [ExportDocumentAndFileType(
        nameType: typeof(FunctionDocumentType),
        modelDefinitionType: Function.FunctionDefinitionType,
        smallImage: "Resources/SketchSmallImgae.xml",
        createNewSmallImage: CreateNewSmallIcon,
        createNewLargeImage: CreateNewLargeIcon,
        paletteImage: PaletteImage,
        relativeImportance: RelativeImportance,
        fileExtension: ".rfn",
        autoCreatesProject: true,
        commandLineArguments: OpenArgumentFormat + ", " + "Foo",
        defaultFileNameType: typeof(FunctionDocumentType))]
    [AllowableAsTypeMember(true)]
    public class FunctionDocumentType : SourceFileDocumentType
    {
        /// <summary>
        /// Our command line
        /// </summary>
        public const string OpenArgument = "openRebarFunction";

        /// <summary>
        /// Our command line
        /// </summary>
        public const string OpenArgumentFormat = OpenArgument + " {0}";

        /// <summary>
        /// Constant for create new large icon.
        /// </summary>
        public const string CreateNewLargeIcon = "SketchCreateNewLargeIcon.png";

        /// <summary>
        /// Constant for create new small icon.
        /// </summary>
        public const string CreateNewSmallIcon = "SketchCreateNewSmallIcon.png";

        /// <summary>
        /// Constant for palette icon.
        /// </summary>
        public const string PaletteImage = "SketchPaletteImage.png";

        /// <summary>
        /// Constant for relative importance.
        /// </summary>
        public const double RelativeImportance = 0.5;

        /// <inheritdoc />
        public override Document CreateDocument(Envoy envoy)
        {
            return Host.CreateInstance<FunctionDocument>();
        }
    }

    /// <summary>
    ///  A Rebar function document
    /// </summary>
    [Export(typeof(FunctionDocument))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class FunctionDocument : DataflowDocument, IProvideOverlayHelp
    {
        private static readonly string _functionDocumentUniqueIdPrefix = "FunctionDocument:";

        /// <summary>
        /// The default name for a new document.
        /// </summary>
        public static readonly string DefaultDocumentName = "Function.rfn";

        /// <summary>
        /// For testing adding command content to different things.
        /// </summary>
        public static readonly ICommandEx DocumentCommand = new ShellRelayCommand(ShellDocumentCommandHelpers.HandleNoop)
        {
            UniqueId = _functionDocumentUniqueIdPrefix + "DocumentCommand",
            LabelTitle = "Document command",
        };

        /// <summary>
        /// For testing adding command content to different things.
        /// </summary>
        public static readonly ICommandEx ElementCommand = new ShellRelayCommand(ShellDocumentCommandHelpers.HandleNoop)
        {
            UniqueId = _functionDocumentUniqueIdPrefix + "ElementCommand",
            LabelTitle = "Document Element command",
        };

        /// <summary>
        /// For testing adding command content to different things.
        /// </summary>
        public static readonly ICommandEx SelectionCommand = new ShellRelayCommand(ShellDocumentCommandHelpers.HandleNoop)
        {
            UniqueId = _functionDocumentUniqueIdPrefix + "SelectionCommand",
            LabelTitle = "Document Selection command",
        };

        /// <summary>
        /// Command tab for Diagnostics operations
        /// </summary>
        public static readonly ICommandEx DiagnosticsTabCommand = new ShellRelayCommand()
        {
            UniqueId = _functionDocumentUniqueIdPrefix + "DiagnosticsTabCommand",
            LabelTitle = "Diagnostics",
        };

        /// <summary>
        /// For testing adding command content to different things.
        /// </summary>
        public static readonly ICommandEx DiagnosticsCommand = new ShellRelayCommand(ShellDocumentCommandHelpers.HandleNoop)
        {
            UniqueId = _functionDocumentUniqueIdPrefix + "DiagnosticsCommand",
            LabelTitle = "Diagnostics command",
        };

        /// <summary>
        /// Command inside the dynamic right rail
        /// </summary>
        public static readonly ICommandEx DynamicPaneCommand = new ShellRelayCommand(ShellDocumentCommandHelpers.HandleNoop)
        {
            UniqueId = _functionDocumentUniqueIdPrefix + "DynamicPaneCommand",
            LabelTitle = "Dynamic Pane command",
        };

        /// <summary>
        ///  The default constructor
        /// </summary>
        public FunctionDocument()
        {
            EditorStateOverride = null;
            // UseIconTemplateEditor = false;
        }

        /// <summary>
        /// Get the associated <see cref="SourceModel.Function"/>.
        /// </summary>
        public Function Function => Definition as Function;

        /// <inheritdoc />
        protected override void CreateCommandContentForDocument(ICommandPresentationContext context)
        {
            base.CreateCommandContentForDocument(context);

            using (context.AddStudioWindowToolBarContent())
            {
                using (context.AddGroup(ShellToolBar.LeftGroupCommand))
                {
                    context.Add(DocumentCommands.Copy);
                    context.Add(DocumentCommands.Paste);
                }
            }

            using (context.AddToolLauncherContent())
            {
                context.Add(DocumentToolCommand);
            }

            using (context.AddConfigurationPaneFooterContent())
            {
                using (context.AddGroup(ConfigurationPaneCommands.FooterCommand, new NColumnLayoutFactory { NumberOfColumns = 3 }))
                {
                    context.Add(DiagnosticsCommand);
                    context.Add(DocumentCommand);
                }
            }
        }

        /// <inheritdoc />
        public override bool ShowConfigurationPane => _showConfigurationPane;

        private bool _showConfigurationPane = true;

        /// <summary>
        /// Sets the value to be returned by the ShowConfigurationPane property.  This is for testing purposes only.
        /// </summary>
        /// <param name="showConfigurationPane">Value to return from ShowConfigurationPane</param>
        public void SetShowConfigurationPane(bool showConfigurationPane)
        {
            _showConfigurationPane = showConfigurationPane;
        }

        /// <summary>
        /// A "Document Tool" command for the document area of the tool launcher
        /// </summary>
        public static ICommandEx DocumentToolCommand = new ShellRelayCommand()
        {
            UniqueId = _functionDocumentUniqueIdPrefix + "DocumentTool",
            LabelTitle = "Document Tool",
        };

#if FALSE
        /// <inheritdoc />
        public override void CreateCommandContentForElement(ICommandPresentationContext context)
        {
            base.CreateCommandContentForElement(context);
            using (context.AddConfigurationPaneContent())
            {
                using (context.AddGroup(ConfigurationPaneCommands.VisualStyleGroupCommand))
                {
                    context.Add(ElementCommand);
                }
            }
        }

        /// <inheritdoc />
        public override void CreateCommandContentForSelection(ICommandPresentationContext context)
        {
            base.CreateCommandContentForElement(context);
            using (context.AddConfigurationPaneContent())
            {
                using (context.AddGroup(ConfigurationPaneCommands.VisualStyleGroupCommand))
                {
                    context.Add(SelectionCommand);
                }
            }
        }

        /// <summary>
        /// For testing adding command content to different things.
        /// </summary>
        public static readonly ICommandEx ContextMenuCommand = new ShellRelayCommand(ShellDocumentCommandHelpers.HandleNoop)
        {
            UniqueId = _functionDocumentUniqueIdPrefix + "ContextMenuCommand",
            LabelTitle = "Document ContextMenu command",
        };

        /// <inheritdoc />
        public override void CreateContextMenuContent(ICommandPresentationContext context, PlatformVisual sourceVisual)
        {
            base.CreateContextMenuContent(context, sourceVisual);
            context.Add(ContextMenuCommand);
        }
#endif

        /// <inheritdoc />
        protected override IEnumerable<IDocumentEditControlInfo> CreateDefaultEditControls()
        {
            yield return new FunctionDiagramEditorInfo(FunctionDiagramEditor.UniqueId, this);

#if FALSE
            if (!OnlyShowSketchDiagramEditor)
            {
                if (UseIconTemplateEditor)
                {
                    var iconInfo = IconTemplateDocumentEditViewModel.CreateDefaultInfo(this, Function.Icon.Model);
                    yield return iconInfo;
                }
                else
                {
                    var iconInfo = new IconAndConnectorPaneDocumentEditControlInfo<IconAndConnectorPaneDocumentEditViewModel, IconAndConnectorPaneDocumentEditControl>(
                        IconAndConnectorPaneDocumentEditControl.IconAndConnectorPaneDocumentEditControlUniqueId,
                        this,
                        Function,
                        "Icon and Connections",
                        IconDocumentEditControl.PaletteIdentifier, // The palette base name
                        "Designer/CommandBar/images/IconEditor_32x32.png",
                        "Designer/CommandBar/images/IconEditor_16x16.png");
                    iconInfo.ClipboardDataFormat = IconDocumentEditControl.ClipboardDataFormat;
                    iconInfo.ShowInSelector = false;
                    yield return iconInfo;
                }
                var panelInfo = new PanelDocumentEditControlInfo<DocumentEditControlViewModel, UIModelSketchEditor>(
                    UIModelSketchEditor.UniqueId,
                    this,
                    null,
                    () => Function.Panel,
                    "UIModel Editor",
                    SketchUtilities.UIModelSketchPaletteIdentifier,
                    string.Empty,
                    string.Empty,
                    string.Empty);
                panelInfo.ClipboardDataFormat = SketchUtilities.UIModelSketchClipboardDataFormat;
                yield return panelInfo;

                var splitDiagramInfo = new SketchDocumentEditorInfo(SketchDocumentEditor.UniqueId, this);
                var splitPanelInfo = new DocumentEditControlInfo<UIModelSketchEditor>(UIModelSketchEditor.UniqueId, this, Function.Panel, "UIModel Editor", SketchUtilities.UIModelSketchPaletteIdentifier, string.Empty, string.Empty);
                splitPanelInfo.ClipboardDataFormat = SketchUtilities.UIModelSketchClipboardDataFormat;
                yield return new SplitEditControlInfo(UIModelSketchEditor.UniqueId, this, null, null, splitPanelInfo, splitDiagramInfo);
            }
#endif
        }

        /// <summary>
        ///  Editor state override
        /// </summary>
        public DocumentCloseEditorState? EditorStateOverride { get; set; }

        /// <summary>
        ///  Can close editors reset event
        /// </summary>
        public AutoResetEvent CanCloseEditorsResetEvent { get; set; }


#if FALSE
        private bool _onlyShowSketchDiagramEditor;

        /// <summary>
        /// True if the sketch document should only show its diagram edit control
        /// </summary>
        public bool OnlyShowSketchDiagramEditor
        {
            get { return _onlyShowSketchDiagramEditor; }
            set
            {
                if (_onlyShowSketchDiagramEditor != value)
                {
                    _onlyShowSketchDiagramEditor = value;
                    RefreshAvailableEditControls();
                }
            }
        }

        /// <summary>
        ///  True if we want to test the icon template editor instead of the icon and connector editor.
        /// </summary>
        public bool UseIconTemplateEditor { get; set; }

        private readonly ICommandEx _showHideEditors = new ShellRelayCommand((p, h, s) =>
        {
            var document = s.ActiveDocument as FunctionDocument;
            document.OnlyShowSketchDiagramEditor = !document.OnlyShowSketchDiagramEditor;
        })
        {
            UniqueId = _functionDocumentUniqueIdPrefix + "ShowHideEditorsCommand",
            LabelTitle = "Show only diagram editor (Show/Hide editors)"
        };        
    
        private BaseWindow _modelessWindow;

        private readonly ICommandEx _showModelessWindow = new ShellRelayCommand((p, h, s) =>
        {
            ((FunctionDocument)s.ActiveDocument).ShowOrFocusModelessWindow();
        })
        {
            UniqueId = _functionDocumentUniqueIdPrefix + "ShowModelessDialogCommand",
            LabelTitle = "Show Modeless Dialog"
        };

        /// <summary>
        /// Shows a modeless dialog
        /// </summary>
        public void ShowOrFocusModelessWindow()
        {
            if (_modelessWindow != null)
            {
                _modelessWindow.Focus();
            }
            else
            {
                _modelessWindow = new BaseWindow
                {
                    Content = "This is a modeless dialog.",
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = MediaTypeNames.Application.Current.MainWindow,
                    Title = "Modeless",
                    Height = 100,
                    Width = 200
                };
                _modelessWindow.Show();
            }
        }

        /// <summary>
        /// Closes the modeless window if one exists
        /// </summary>
        public void CloseModelessWindow()
        {
            _modelessWindow?.Close();
            _modelessWindow = null;
        }

    /// <summary>
        /// Command for toggling the document header
        /// </summary>
        private readonly ICommandEx _toggleHeader = new ShellRelayCommand(ChangeHeader)
        {
            UniqueId = _functionDocumentUniqueIdPrefix + "ToggleHeader",
            LabelTitle = "Show/hide header",
        }.SetParameter("ToggleHeader");

        /// <summary>
        /// Command for toggling the document header's done button
        /// </summary>
        private readonly ICommandEx _toggleDone = new ShellRelayCommand(ChangeHeader)
        {
            UniqueId = _functionDocumentUniqueIdPrefix + "ToggleDone",
            LabelTitle = "Show/hide done button",
        }.SetParameter("ToggleDone");

        /// <summary>
        /// Command for changing the document header's text
        /// </summary>
        private readonly ICommandEx _changeText = new ShellRelayCommand(ChangeHeader)
        {
            UniqueId = _functionDocumentUniqueIdPrefix + "ChangeText",
            LabelTitle = "Text",
        }.SetParameter("ChangeText");

        private bool _showHideDoneButton = true;
        private string _headerText = "This is the header.";
        private Guid _alertId = Guid.Empty;

        private static void ChangeHeader(ICommandParameter parameter, ICompositionHost host, DocumentEditSite site)
        {
            var document = (FunctionDocument)site.ActiveDocument;
            bool showHeader = true;

            switch ((string)parameter.Parameter)
            {
                case "ToggleHeader":
                    showHeader = document._alertId == Guid.Empty;
                    break;
                case "ToggleDone":
                    document._showHideDoneButton ^= true;
                    break;
                case "ChangeText":
                    document._headerText = TextCommandParameter.GetText(parameter);
                    break;
            }

            site.ActiveDocumentEditor.RemoveAlert(document._alertId);
            document._alertId = Guid.Empty;
            if (showHeader)
            {
                string tooltip = "Here you will see additional information";
                document._alertId = site.ActiveDocumentEditor.AddAlert(null, document._headerText, tooltip, null, document._showHideDoneButton);
            }
        }
#endif

        /// <inheritdoc />
        public override DocumentCloseEditorState CanCloseEditors(IEnumerable<DocumentEditControl> editors, CloseType closeType)
        {
            if (EditorStateOverride != null)
            {
                DocumentCloseEditorState returnValue = EditorStateOverride.Value == DocumentCloseEditorState.WaitForDelegate
                                                       ? DocumentCloseEditorState.WaitForDelegate
                                                       : EditorStateOverride.Value;
                CanCloseEditorsResetEvent?.Set();
                return returnValue;
            }
            CanCloseEditorsResetEvent?.Set();
            return base.CanCloseEditors(editors, closeType);
        }

        /// <inheritdoc />
        public override IEnumerable<DocumentOverlayHelpContent> GetOverlayHelpContent()
        {
            return new List<DocumentOverlayHelpContent>
            {
                new DocumentOverlayHelpContent(OverlayHelpProviderVisualId.DocumentDescription, "NI.OverlayHelp:SketchDocument".NotLocalized(), OverlayHelpTitle)
            };
        }

        /// <summary>
        /// The title for the overlay help Control.
        /// </summary>
        public const string OverlayHelpTitle = "Rebar Function";

        /// <summary>
        /// Our identifier
        /// </summary>
        public const string Identifier = "FunctionDocument";

        /// <summary>
        /// Title for document right rail.
        /// </summary>
        public const string ConfigurationPaneTitle = "Rebar Function";

        /// <inheritdoc />
        public override IEnumerable<BindingKeyword> BindingKeywords => base.BindingKeywords.Concat(new BindingKeyword[] { Identifier });

        /// <inheritdoc />
        protected override Type DiagramControlType => typeof(FunctionDiagramEditor);

#if FALSE
        private ImageSource _statusMessageIcon;
        private string _statusMessage;
        private string _statusMessageTipStrip;

        /// <summary>
        /// Used to set document status information that will be displayed when a DocumentEditor is displayed.
        /// </summary>
        /// <param name="icon">The status message</param>
        /// <param name="message">The status message</param>
        /// <param name="tipStrip">The status message tip strip</param>
        public void SetInitialStatusMessageInfo(ImageSource icon, string message, string tipStrip)
        {
            _statusMessageIcon = icon;
            _statusMessage = message;
            _statusMessageTipStrip = tipStrip;
        }
#endif

        /// <inheritdoc />
        protected override void OnEditorAdded(DocumentEditControl editControl)
        {
            base.OnEditorAdded(editControl);
#if FALSE
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                editControl.AddAlert(_statusMessageIcon, _statusMessage, _statusMessageTipStrip);
            }
#endif
        }
    }
}
