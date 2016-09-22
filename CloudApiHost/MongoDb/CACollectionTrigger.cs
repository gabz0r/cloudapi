using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CloudApiLib.Documents;

namespace CloudApiHost.MongoDb
{
    public class CACollectionTrigger
    {
        public delegate string CreateTriggerDelegate(object iNewDocument);
        public delegate string UpdateTriggerDelegate(object iOldDocument, object iNewDocument);
        public delegate string DeleteTriggerDelegate(object iOldValue);

        public CreateTriggerDelegate PreCreate { get; set; }
        public CreateTriggerDelegate PostCreate { get; set; }

        public UpdateTriggerDelegate PreUpdate { get; set; }
        public UpdateTriggerDelegate PostUpdate { get; set; }

        public DeleteTriggerDelegate PreDelete { get; set; }
        public DeleteTriggerDelegate PostDelete { get; set; }
    }
}
