using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleApp3
{
    class Program
    {
        static void Main(string[] args)
        {
            var testStr = @"UUID:           9f402700-4ab0-4d09-8429-0c9974f203bf
Parent UUID:    base
State:          created
Type:           normal (base)
Location:       Y:\VirtualboxVms\micronbase\centos-7-1-1.x86_64.vmdk
Storage format: VMDK
Capacity:       40960 MBytes
Encryption:     disabled

UUID:           778cfdc8-c482-4ed1-9410-df225914ab97
Parent UUID:    base
State:          created
Type:           normal (base)
Location:       Y:\VirtualboxVms\micronw7base_1499294072087_17502\box-disk001.vmdk
Storage format: VMDK
Capacity:       51200 MBytes
Encryption:     disabled

UUID:           debc84da-4ce3-4b06-b375-62fb5923b664
Parent UUID:    778cfdc8-c482-4ed1-9410-df225914ab97
State:          created
Type:           normal (differencing)
Location:       Y:\VirtualboxVms\micronw7base_1499294072087_17502\Snapshots/{debc84da-4ce3-4b06-b375-62fb5923b664}.vmdk
Storage format: VMDK
Capacity:       51200 MBytes
Encryption:     disabled

UUID:           debc84da-4ce3-4b06-b375-62fb5923b664
Parent UUID:    778cfdc8-c482-4ed1-9410-df225914ab97
State:          created
Type:           normal (differencing)
Location:       Y:\VirtualboxVms\micronw7base_test_17502\Snapshots/{debc84da-4ce3-4b06-b375-62fb5923b664}.vmdk
Storage format: VMDK
Capacity:       51200 MBytes
Encryption:     disabled

UUID:           29649291-2bf0-4ab5-9cf1-93d5d47e8cc1
Parent UUID:    778cfdc8-c482-4ed1-9410-df225914ab97
State:          inaccessible
Type:           normal (differencing)
Location:       Y:\VirtualboxVms\micronw7base\Snapshots\{29649291-2bf0-4ab5-9cf1-93d5d47e8cc1}.vmdk
Storage format: VMDK
Capacity:       0 MBytes
Encryption:     disabled";

            var vboxmanage = new Vboxmange();
            var hdds = vboxmanage.GetTipHdds(testStr, "micronw7base").ToList();

            Console.Read();
        }
    }
}
