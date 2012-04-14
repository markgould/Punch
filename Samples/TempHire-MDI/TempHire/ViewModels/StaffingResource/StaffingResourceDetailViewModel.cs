// ====================================================================================================================
//   Copyright (c) 2012 IdeaBlade
// ====================================================================================================================
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE 
//   WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS 
//   OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR 
//   OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
// ====================================================================================================================
//   USE OF THIS SOFTWARE IS GOVERENED BY THE LICENSING TERMS WHICH CAN BE FOUND AT
//   http://cocktail.ideablade.com/licensing
// ====================================================================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using Caliburn.Micro;
using Cocktail;
using Common.Errors;
using Common.Factories;
using Common.Messages;
using DomainServices;
using IdeaBlade.Core;

#if HARNESS
using DomainServices.SampleData;
#endif

namespace TempHire.ViewModels.StaffingResource
{
    [Export, PartCreationPolicy(CreationPolicy.NonShared)]
    public class StaffingResourceDetailViewModel : Conductor<IScreen>.Collection.OneActive, IDiscoverableViewModel,
                                                   IHarnessAware, IHandle<EntityChangedMessage>
    {
        private readonly IDialogManager _dialogManager;
        private readonly IErrorHandler _errorHandler;
        private readonly IEnumerable<IStaffingResourceDetailSection> _sections;
        private readonly IDomainUnitOfWorkManager<IDomainUnitOfWork> _unitOfWorkManager;
        private DomainModel.StaffingResource _staffingResource;
        private Guid _staffingResourceId;
        private IDomainUnitOfWork _unitOfWork;

        [ImportingConstructor]
        public StaffingResourceDetailViewModel(IDomainUnitOfWorkManager<IDomainUnitOfWork> unitOfWorkManager,
                                               StaffingResourceSummaryViewModel staffingResourceSummary,
                                               [ImportMany] IEnumerable<IStaffingResourceDetailSection> sections,
                                               IErrorHandler errorHandler, IDialogManager dialogManager)
        {
            StaffingResourceSummary = staffingResourceSummary;
            _unitOfWorkManager = unitOfWorkManager;
            _sections = sections.ToList();
            _errorHandler = errorHandler;
            _dialogManager = dialogManager;
            Busy = new BusyWatcher();

            PropertyChanged += OnPropertyChanged;

// ReSharper disable DoNotCallOverridableMethodsInConstructor
            DisplayName = "Loading...";
// ReSharper restore DoNotCallOverridableMethodsInConstructor
        }

        public StaffingResourceSummaryViewModel StaffingResourceSummary { get; private set; }

        public IBusyWatcher Busy { get; private set; }

        public bool Visible
        {
            get { return StaffingResource != null; }
        }

        public IDomainUnitOfWork UnitOfWork
        {
            get { return _unitOfWork ?? (_unitOfWork = _unitOfWorkManager.Get(_staffingResourceId)); }
        }

        public int ActiveSectionIndex
        {
            get { return Items.IndexOf(ActiveItem); }
            set { ActivateItem(Items[Math.Max(value, 0)]); }
        }

        public DomainModel.StaffingResource StaffingResource
        {
            get { return _staffingResource; }
            set
            {
                _staffingResource = value;
                NotifyOfPropertyChange(() => StaffingResource);
                NotifyOfPropertyChange(() => Visible);
                UpdateDisplayName();
            }
        }

        #region IHandle<EntityChangedMessage> Members

        void IHandle<EntityChangedMessage>.Handle(EntityChangedMessage message)
        {
            if (StaffingResource == null || !UnitOfWork.HasEntity(message.Entity)) 
                return;

            UpdateDisplayName();
        }

        #endregion

        #region IHarnessAware Members

        public void Setup()
        {
#if HARNESS
    //Start("John", "M.", "Doe");
            Start(TempHireSampleDataProvider.CreateGuid(1));
#endif
        }

        #endregion

        public void UpdateDisplayName()
        {
            if (StaffingResource == null) return;
            DisplayName = StaffingResource.FullName + (UnitOfWork.HasChanges() ? "*" : "");
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ActiveItem")
                NotifyOfPropertyChange(() => ActiveSectionIndex);
        }

        public StaffingResourceDetailViewModel Start(Guid staffingResourceId,
                                                     Action<StaffingResourceDetailViewModel> callback = null)
        {
            Busy.AddWatch();

            _unitOfWork = null;
            _staffingResourceId = staffingResourceId;
            // Bring resource into cache and defer starting of nested VMs until completed.
            UnitOfWork.StaffingResources.WithIdAsync(staffingResourceId,
                                                     resource => OnStartCompleted(resource, callback),
                                                     _errorHandler.HandleError)
                .OnComplete(args => Busy.RemoveWatch());

            return this;
        }

        private void OnStartCompleted(DomainModel.StaffingResource staffingResource,
                                      Action<StaffingResourceDetailViewModel> callback = null)
        {
            StaffingResource = staffingResource;
            StaffingResourceSummary.Start(staffingResource.Id);

            _sections.ForEach(s => s.Start(staffingResource.Id));
            if (Items.Count == 0)
            {
                Items.AddRange(_sections.OrderBy(s => s.Index).Cast<IScreen>());
                NotifyOfPropertyChange(() => Items);
                ActivateItem(Items.First());
            }

            if (callback != null)
                callback(this);
        }

        public StaffingResourceDetailViewModel Start(string firstName, string middleName, string lastName,
                                                     Action<StaffingResourceDetailViewModel> callback = null)
        {
            Busy.AddWatch();

            _unitOfWork = _unitOfWorkManager.Create();
            _unitOfWork.StaffingResourceFactory.CreateAsync(firstName, middleName, lastName,
                                                            resource =>
                                                                {
                                                                    _unitOfWorkManager.Add(resource.Id, _unitOfWork);
                                                                    Start(resource.Id, callback);
                                                                },
                                                            _errorHandler.HandleError)
                .OnComplete(args => Busy.RemoveWatch());

            return this;
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            ((IActivate) StaffingResourceSummary).Activate();
        }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);
            ((IDeactivate) StaffingResourceSummary).Deactivate(close);

            if (close)
            {
                StaffingResource = null;
                _unitOfWork = null;
            }
        }

        public override void CanClose(Action<bool> callback)
        {
            if (UnitOfWork.HasChanges())
            {
                var dialogResult =
                    _dialogManager.ShowMessage(
                        string.Format("Do you want to save changes you made to {0}?", StaffingResource.FullName),
                        DialogResult.Yes, DialogResult.Cancel, DialogButtons.YesNoCancel);
                dialogResult.OnComplete(delegate
                                            {
                                                if (dialogResult.DialogResult == DialogResult.Yes)
                                                {
                                                    Busy.AddWatch();
                                                    UnitOfWork.CommitAsync(saveResult => callback(true),
                                                                           _errorHandler.HandleError)
                                                        .OnComplete(args => Busy.RemoveWatch());
                                                }

                                                if (dialogResult.DialogResult == DialogResult.No)
                                                {
                                                    UnitOfWork.Rollback();
                                                    callback(true);
                                                }

                                                if (dialogResult.DialogResult == DialogResult.Cancel)
                                                    callback(false);
                                            });
            }
            else
                base.CanClose(callback);
        }
    }

    [Export(typeof(IPartFactory<StaffingResourceDetailViewModel>))]
    public class StaffingResourceDetailViewModelFactory : PartFactoryBase<StaffingResourceDetailViewModel>
    {
    }
}