
local cabinet = {}

cabinet.name = "Head2Head/ArcadeCabinet"

function cabinet.texture()
    return "Head2Head/ArcadeCabinet/FourInARow00"
end

cabinet.placements = {
    name = "FourInARow",
    data = {
       game = "FourInARow",
    },
}

return cabinet