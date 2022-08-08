module Head2HeadHelpdesk

using ..Ahorn, Maple

@mapdef Entity "Head2Head/Helpdesk" Helpdesk(x::Integer, y::Integer)

const placements = Ahorn.PlacementDict(
    "Helpdesk (Head2Head)" => Ahorn.EntityPlacement(
        Helpdesk
    )
)

sprite = "Head2Head/Helpdesk/Idle00.png"

function Ahorn.selection(entity::Helpdesk)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::Helpdesk, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end