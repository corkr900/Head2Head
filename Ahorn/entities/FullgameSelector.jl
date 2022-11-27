module Head2HeadFullgameSelector

using ..Ahorn, Maple

@mapdef Entity "Head2Head/FullgameSelector" FullgameSelector(x::Integer, y::Integer)

const placements = Ahorn.PlacementDict(
    "Fullgame Selector (Head2Head)" => Ahorn.EntityPlacement(
        FullgameSelector
    )
)

sprite = "Head2Head/FullgameSelector/Idle00.png"

function Ahorn.selection(entity::FullgameSelector)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::FullgameSelector, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end