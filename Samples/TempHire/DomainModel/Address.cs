using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Cocktail;
using IdeaBlade.EntityModel;
using IdeaBlade.Validation;

namespace DomainModel
{
    [DataContract(IsReference = true)]
    public class Address : EntityBase, IHasRoot
    {
        internal Address()
        {
        }

        /// <summary>Gets or sets the Id. </summary>
        [DataMember]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Required]
        public Guid Id { get; internal set; }

        /// <summary>Gets or sets the Address1. </summary>
        [DataMember]
        [Required]
        public string Address1 { get; set; }

        /// <summary>Gets or sets the Address2. </summary>
        [DataMember]
        public string Address2 { get; set; }

        /// <summary>Gets or sets the City. </summary>
        [DataMember]
        [Required]
        public string City { get; set; }

        /// <summary>Gets or sets the ResourceId. </summary>
        [DataMember]
        [Required]
        public Guid ResourceId { get; set; }

        /// <summary>Gets or sets the AddressTypeId. </summary>
        [DataMember]
        [Required]
        public Guid AddressTypeId { get; set; }

        /// <summary>Gets or sets the Zipcode. </summary>
        [DataMember]
        [Required]
        [StringLength(10, MinimumLength = 5)]
        public string Zipcode { get; set; }

        /// <summary>Gets or sets the Primary. </summary>
        [DataMember]
        [Required]
        public bool Primary { get; set; }

        /// <summary>Gets or sets the StateId. </summary>
        [DataMember]
        [Required]
        public Guid StateId { get; set; }

        /// <summary>Gets or sets the Resource. </summary>
        [DataMember]
        public Resource Resource { get; set; }

        /// <summary>Gets or sets the AddressType. </summary>
        [DataMember]
        public AddressType AddressType { get; set; }

        /// <summary>Gets or sets the State. </summary>
        [DataMember]
        [Required]
        public State State { get; set; }

        #region IHasRoot Members

        public object Root
        {
            get { return Resource; }
        }

        #endregion

        #region EntityPropertyNames

        public class EntityPropertyNames : Entity.EntityPropertyNames
        {
            public const String Id = "Id";
            public const String Address1 = "Address1";
            public const String Address2 = "Address2";
            public const String City = "City";
            public const String ResourceId = "ResourceId";
            public const String AddressTypeId = "AddressTypeId";
            public const String Zipcode = "Zipcode";
            public const String Primary = "Primary";
            public const String StateId = "StateId";
            public const String Resource = "Resource";
            public const String AddressType = "AddressType";
            public const String PrimaryResources = "PrimaryResources";
            public const String State = "State";
        }

        #endregion EntityPropertyNames

        internal static Address Create(AddressType type)
        {
            return new Address { Id = CombGuid.NewGuid(), AddressTypeId = type.Id };
        }

        public override void Validate(VerifierResultCollection validationErrors)
        {
            if (State.EntityFacts.EntityAspect.IsNullOrPendingEntity)
                validationErrors.Add(new VerifierResult(VerifierResultCode.Error, "The State field is required", "State"));
        }
    }
}