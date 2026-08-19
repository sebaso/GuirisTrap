using System;

/// <summary>
/// Etiquetas de contenido de un plato. Es un [Flags]: un plato puede tener
/// varias a la vez (ej. la Paella = Fish | Meat).
///
/// Las usan los clientes especiales para vetar categorías enteras:
/// Poseidón rechaza Fish, las Gaviotas rechazarán Especias, etc.
/// Se asignan en cada asset de RecipeData (campo "tags").
/// </summary>
[Flags]
public enum FoodTag
{
    None     = 0,
    Fish     = 1 << 0,  // pescado / marisco
    Especias = 1 << 1,  // platos del mortero de especias
    Meat     = 1 << 2,  // carne
    Veggie   = 1 << 3,  // vegetal
}
