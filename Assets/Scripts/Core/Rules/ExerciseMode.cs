namespace Entrenamiento.Core.Rules
{
    /// <summary>
    /// Presets de entrenamiento disponibles para el host.
    /// </summary>
    public enum ExerciseMode
    {
        Reaction = 0,
        AllSame = 1,
        Colors = 2,
        Decision = 3,
        CognitiveFake = 4,
        Football = 5
    }

    /// <summary>
    /// Selección global de la portada/configuración. SessionConfig toma una
    /// instantánea de este valor cuando se crea una sesión.
    /// </summary>
    public static class ExerciseSelection
    {
        public static ExerciseMode Current { get; set; } = ExerciseMode.Reaction;

        public static string Name(ExerciseMode mode)
        {
            switch (mode)
            {
                case ExerciseMode.Reaction: return "REACCIÓN";
                case ExerciseMode.AllSame: return "TODOS IGUALES";
                case ExerciseMode.Colors: return "COLORES";
                case ExerciseMode.Decision: return "DECISIÓN";
                case ExerciseMode.CognitiveFake: return "FINTA COGNITIVA";
                case ExerciseMode.Football: return "FÚTBOL";
                default: return mode.ToString().ToUpperInvariant();
            }
        }

        public static string Rule(ExerciseMode mode)
        {
            switch (mode)
            {
                case ExerciseMode.Reaction:
                    return "UN POD VERDE AL AZAR";
                case ExerciseMode.AllSame:
                    return "TODOS AZULES · APAGALOS TODOS";
                case ExerciseMode.Colors:
                    return "4 COLORES · TOCÁ SOLO EL INDICADO";
                case ExerciseMode.Decision:
                    return "VERDE ↑ · ROJO ↓ · AZUL ← · AMARILLO →";
                case ExerciseMode.CognitiveFake:
                    return "EL COLOR CAMBIA DURANTE LA APROXIMACIÓN";
                case ExerciseMode.Football:
                    return "VERDE DERECHO · AZUL IZQUIERDO · ROJO QUIETO";
                default:
                    return string.Empty;
            }
        }
    }

    /// <summary>
    /// Puente runtime para que la capa Unity pueda observar el coordinador puro
    /// sin acoplar Core a MonoBehaviour.
    /// </summary>
    public static class ExerciseRuntimeRegistry
    {
        public static SessionCoordinator CurrentCoordinator { get; internal set; }
    }
}
