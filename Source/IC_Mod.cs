using UnityEngine;
using Verse;

namespace IronCradle
{
    /// <summary>
    /// Punto de entrada del mod Iron Cradle.
    /// El atributo [StaticConstructorOnStartup] garantiza que el bloque estático
    /// se ejecuta una sola vez cuando RimWorld termina de cargar todos los mods.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class IC_Mod
    {
        static IC_Mod()
        {
            Log.Message("[IronCradle] Mod cargado correctamente.");

            // Advertencia temprana si falta la textura principal del edificio.
            // ContentFinder devuelve null (no lanza excepción) cuando reportFailure=false.
            if (ContentFinder<Texture2D>.Get("Things/Buildings/IronCradle", false) == null)
            {
                Log.Warning(
                    "[IronCradle] Textura no encontrada. " +
                    "Coloca IronCradle.png (128×128 px) en " +
                    "Textures/Things/Buildings/ para eliminar este aviso.");
            }
        }
    }
}
