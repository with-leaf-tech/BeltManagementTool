using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeltManagementTool.Entity {
    abstract public class EntityBase {

        public Dictionary<string, bool> canEquip { set; get; }

        public EntityBase(string value) {
            TextToEntity(value);
        }

        public virtual void TextToEntity(string value) {

        }
    }
}
