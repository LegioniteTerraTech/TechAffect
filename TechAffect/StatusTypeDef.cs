using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AffectTech
{

    public enum StatusTypeDef
    {
        NULL,
        // Beneficial
        FilmShield,
        Overclock,
        // Mutual
        Overheat,
        Freezing,
        // Detrimental
        EMF,
        Jamming,
        Hacked,
        Acid,
        Pry,        // Cutting weapons
    }
}
