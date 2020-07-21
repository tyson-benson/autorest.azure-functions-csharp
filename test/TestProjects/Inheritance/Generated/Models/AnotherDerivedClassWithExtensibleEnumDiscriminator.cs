// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// <auto-generated/>

#nullable disable

namespace Inheritance.Models
{
    /// <summary> The AnotherDerivedClassWithExtensibleEnumDiscriminator. </summary>
    internal partial class AnotherDerivedClassWithExtensibleEnumDiscriminator : BaseClassWithExtensibleEnumDiscriminator
    {
        /// <summary> Initializes a new instance of AnotherDerivedClassWithExtensibleEnumDiscriminator. </summary>
        internal AnotherDerivedClassWithExtensibleEnumDiscriminator()
        {
            DiscriminatorProperty = new BaseClassWithEntensibleEnumDiscriminatorEnum("random value");
        }

        /// <summary> Initializes a new instance of AnotherDerivedClassWithExtensibleEnumDiscriminator. </summary>
        /// <param name="discriminatorProperty"> . </param>
        internal AnotherDerivedClassWithExtensibleEnumDiscriminator(BaseClassWithEntensibleEnumDiscriminatorEnum discriminatorProperty) : base(discriminatorProperty)
        {
            DiscriminatorProperty = discriminatorProperty;
        }
    }
}