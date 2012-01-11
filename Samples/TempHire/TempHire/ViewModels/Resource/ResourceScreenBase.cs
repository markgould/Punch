using System;
using Caliburn.Micro;
using Cocktail;
using Common.Errors;
using Common.Repositories;
using Common.SampleData;
using TempHire.Repositories;

namespace TempHire.ViewModels.Resource
{
    public abstract class ResourceScreenBase : Screen, IDiscoverableViewModel, IHarnessAware
    {
        private IResourceRepository _repository;
        private DomainModel.Resource _resource;
        private Guid _resourceId;

        protected ResourceScreenBase(IRepositoryManager<IResourceRepository> repositoryManager,
                                     IErrorHandler errorHandler)
        {
            RepositoryManager = repositoryManager;
            ErrorHandler = errorHandler;
        }

        public IRepositoryManager<IResourceRepository> RepositoryManager { get; private set; }
        public IErrorHandler ErrorHandler { get; private set; }

        protected IResourceRepository Repository
        {
            get { return _repository ?? (_repository = RepositoryManager.GetRepository(_resourceId)); }
        }

        public virtual DomainModel.Resource Resource
        {
            get { return _resource; }
            set
            {
                _resource = value;
                NotifyOfPropertyChange(() => Resource);
            }
        }

        #region IHarnessAware Members

        public void Setup()
        {
#if HARNESS
            Start(TempHireSampleDataProvider.CreateGuid(1));
#endif
        }

        #endregion

        public virtual ResourceScreenBase Start(Guid resourceId)
        {
            _repository = null;
            _resourceId = resourceId;
            Repository.GetResourceAsync(resourceId, result => Resource = result, ErrorHandler.HandleError);
            return this;
        }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);

            if (close)
            {
                Resource = null;
                _repository = null;
            }
        }
    }
}