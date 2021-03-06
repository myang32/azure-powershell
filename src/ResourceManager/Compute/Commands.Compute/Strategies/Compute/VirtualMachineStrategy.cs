﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.Management.Internal.Resources.Models;
using Microsoft.Azure.Commands.Compute.Strategies.ResourceManager;
using Microsoft.Azure.Management.Internal.Network.Version2017_10_01.Models;

namespace Microsoft.Azure.Commands.Common.Strategies.Compute
{
    static class VirtualMachineStrategy
    {
        public static ResourceStrategy<VirtualMachine> Strategy { get; }
            = ComputePolicy.Create(
                type: "virtual machine",
                provider: "virtualMachines",
                getOperations: client => client.VirtualMachines,
                getAsync: (o, p) => o.GetAsync(
                    p.ResourceGroupName, p.Name, null, p.CancellationToken),
                createOrUpdateAsync: (o, p) => o.CreateOrUpdateAsync(
                    p.ResourceGroupName, p.Name, p.Model, p.CancellationToken),
                createTime: c => c.OsProfile.WindowsConfiguration != null ? 240 : 120);

        public static ResourceConfig<VirtualMachine> CreateVirtualMachineConfig(
            this ResourceConfig<ResourceGroup> resourceGroup,
            string name,
            ResourceConfig<NetworkInterface> networkInterface,
            bool isWindows,
            string adminUsername,
            string adminPassword,
            Image image,
            string size)
            => Strategy.CreateResourceConfig(
                resourceGroup: resourceGroup,
                name: name, 
                createModel: subscription => new VirtualMachine
                {
                    OsProfile = new OSProfile
                    {
                        ComputerName = name,
                        WindowsConfiguration = isWindows ? new WindowsConfiguration { } : null,
                        LinuxConfiguration = isWindows ? null : new LinuxConfiguration(),
                        AdminUsername = adminUsername,
                        AdminPassword = adminPassword,
                    },
                    NetworkProfile = new NetworkProfile
                    {
                        NetworkInterfaces = new[]
                        {
                            new NetworkInterfaceReference
                            {
                                Id = networkInterface.GetId(subscription).IdToString()
                            }
                        }
                    },
                    HardwareProfile = new HardwareProfile
                    {
                        VmSize = size
                    },
                    StorageProfile = new StorageProfile
                    {
                        ImageReference = new ImageReference
                        {
                            Publisher = image.publisher,
                            Offer = image.offer,
                            Sku = image.sku,
                            Version = image.version
                        }
                    },
                },
                dependencies: new[] { networkInterface });
    }
}
