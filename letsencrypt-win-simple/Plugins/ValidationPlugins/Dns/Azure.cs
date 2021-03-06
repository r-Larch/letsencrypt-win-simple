﻿using LetsEncrypt.ACME.Simple.Services;
using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;
using Microsoft.Rest.Azure.Authentication;
using Nager.PublicSuffix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Dns
{
    class Azure : DnsValidation
    {
        private static DomainParser _domainParser;

        public Azure() { }
        public Azure(Target target)
        {
            // Build the service credentials and DNS management client
            var serviceCreds = ApplicationTokenProvider.LoginSilentAsync(
                target.DnsAzureOptions.TenantId,
                target.DnsAzureOptions.ClientId,
                target.DnsAzureOptions.Secret).Result;
            _DnsClient = new DnsManagementClient(serviceCreds) {
                SubscriptionId = target.DnsAzureOptions.SubscriptionId
            };
            if (_domainParser == null)
            {
                _domainParser = new DomainParser(new WebTldRuleProvider());
            }
        }

        public override IValidationPlugin CreateInstance(Target target)
        {
            return new Azure(target);
        }

        private DnsManagementClient _DnsClient;
        public override string Name => nameof(Azure);
        public override string Description => "Azure DNS";

        public override void CreateRecord(Target target, string identifier, string recordName, string token)
        {
            var url = _domainParser.Get(recordName);

            // Create record set parameters
            var recordSetParams = new RecordSet
            {
                TTL = 3600,
                TxtRecords = new List<TxtRecord>
                {
                    new TxtRecord(new[] { token })
                }
            };

            _DnsClient.RecordSets.CreateOrUpdate(target.DnsAzureOptions.ResourceGroupName, 
                url.RegistrableDomain,
                url.SubDomain,
                RecordType.TXT, 
                recordSetParams);
        }

        public override void DeleteRecord(Target target, string identifier, string recordName)
        {
            var url = _domainParser.Get(recordName);
            _DnsClient.RecordSets.Delete(target.DnsAzureOptions.ResourceGroupName, url.RegistrableDomain, url.SubDomain, RecordType.TXT);
        }

        public override void Aquire(OptionsService options, InputService input, Target target)
        {
            target.DnsAzureOptions = new AzureDnsOptions(options, input);
        }

        public override void Default(OptionsService options, Target target)
        {
            target.DnsAzureOptions = new AzureDnsOptions(options);
        }
    }
}
