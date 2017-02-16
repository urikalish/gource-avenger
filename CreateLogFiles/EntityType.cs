using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Avenger
{
    public enum EntityType
    {
        Defect,
        Requirement,
        Test
    }

    internal static class EntityTypeHelper
    {
        internal static int GetModuleIdByEntityType(EntityType et)
        {
            switch (et)
            {
                case EntityType.Defect:
                    return 0;
                case EntityType.Requirement:
                    return 3;
                case EntityType.Test:
                    return 2; //TestLab
                default:
                    return 0;
            }
            
        }
    }
}
