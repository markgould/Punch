using Cocktail;
using DomainModel;

namespace TempHire.EntityManagerProviders
{
    public class DesignTimeEntityManagerProvider : BaseDesignTimeEntityManagerProvider<TempHireEntities>
    {
        public DesignTimeEntityManagerProvider(ISampleDataProvider<TempHireEntities>[] sampleDataProviders)
            : base(sampleDataProviders)
        {
        }

        protected override TempHireEntities CreateEntityManager()
        {
            return new TempHireEntities(false);
        }
    }
}