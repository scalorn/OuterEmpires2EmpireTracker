using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.ViewModels;
using NLog;
using System;
using System.Windows.Forms;

namespace OE2EmpireTracker
{
    /// <summary>
    /// FormBlueprintV2 — Clean rewrite of the blueprint management form.
    /// Built around write-through: the data model (PropertyBag, Resources) is always
    /// the source of truth. The grid is a view, not a store.
    /// </summary>
    public partial class FormBlueprintV2 : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private int _isProgrammaticUpdate = 0;
        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }
        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        private EmpireContext empireContext;
        private PlayerContext playerContext;
        private BlueprintViewModel viewModel;

        public FormBlueprintV2()
        {
            InitializeComponent();

            empireContext = EmpireContext.GetInstance();
            playerContext = EmpireContext.PlayerContext;
            viewModel = new BlueprintViewModel(new Blueprint(), playerContext);
        }
    }
}
