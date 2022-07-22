module Head2HeadMatchSwitch

using ..Ahorn, Maple

@mapdef Entity "Head2Head/MatchSwitch" MatchSwitch(x::Integer, y::Integer, action::String="Join")

const placements = Ahorn.PlacementDict(
    "Match Control Switch (Head2Head)" => Ahorn.EntityPlacement(
        MatchSwitch
    )
)

Ahorn.editingOptions(entity::MatchSwitch) = Dict{String, Any}(
    "action" => String["Stage", "Join", "Start"]
)

function Ahorn.selection(entity::MatchSwitch)
    x, y = Ahorn.position(entity)
    sprite = "Head2Head/MatchSwitch/" * get!(entity, "action", "Join") * "00.png"
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::MatchSwitch, room::Maple.Room)
    sprite = "Head2Head/MatchSwitch/" * get!(entity, "action", "Join") * "00.png"
    Ahorn.drawSprite(ctx, sprite, 0, 0)
end

end