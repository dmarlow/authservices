﻿using Kentor.AuthServices.Configuration;
using System;
using System.Collections.Generic;
using System.IdentityModel.Metadata;
using System.Linq;
using System.Text;

namespace Kentor.AuthServices
{
    class Federation
    {
        readonly IDictionary<EntityId, IdentityProvider> identityProviders;

        public IDictionary<EntityId, IdentityProvider> IdentityProviders
        {
            get
            {
                return identityProviders;
            }
        }

        public Federation(Uri metadataUrl, bool allowUnsolicitedAuthnResponse)
            : this(MetadataLoader.LoadFederation(metadataUrl), allowUnsolicitedAuthnResponse)
        {
        }

        internal Federation(EntitiesDescriptor metadata, bool allowUnsolicitedAuthnResponse)
        {
            identityProviders = metadata.ChildEntities
                .Where(ed => ed.RoleDescriptors.OfType<IdentityProviderSingleSignOnDescriptor>().Any())
                .Select(ed => new IdentityProvider(ed, allowUnsolicitedAuthnResponse))
                .ToDictionary(idp => idp.EntityId, EntityIdEqualityComparer.Instance);
        }
    }
}
 