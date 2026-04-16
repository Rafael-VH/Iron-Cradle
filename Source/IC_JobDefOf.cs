using RimWorld;
using Verse;

namespace IronCradle
{
    /// <summary>
    /// Registro estático de los JobDefs propios del mod.
    /// El atributo [DefOf] hace que RimWorld inyecte automáticamente las referencias
    /// después de que todas las definiciones XML han sido cargadas.
    /// </summary>
    [DefOf]
    public static class IC_JobDefOf
    {
        /// <summary>
        /// Job de navegación: el mecanoide se desplaza hasta la InteractionCell de la estación.
        /// Emitido por JobGiver_GoToIronCradle, ejecutado por JobDriver_GoToIronCradle.
        /// </summary>
        public static JobDef IC_GoToIronCradle;

        /// <summary>
        /// Job de reparación: el mecanoide permanece en la estación mientras CompIronCradle
        /// aplica curación tick a tick. Termina cuando CurrentOccupant pasa a null.
        /// </summary>
        public static JobDef IC_RepairAtIronCradle;

        static IC_JobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(IC_JobDefOf));
        }
    }
}
