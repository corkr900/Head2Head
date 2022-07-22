module Head2HeadILSelector

using ..Ahorn, Maple

@mapdef Entity "Head2Head/ILSelector" ILSelector(x::Integer, y::Integer)

const placements = Ahorn.PlacementDict(
    "IL Selector (Head2Head)" => Ahorn.EntityPlacement(
        ILSelector
    )
)

sprite = "Head2Head/ILSelector/Idle00.png"

function Ahorn.selection(entity::ILSelector)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::ILSelector, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end