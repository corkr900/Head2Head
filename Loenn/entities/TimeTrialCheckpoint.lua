
local utils = require("utils")

local timeTrialCheckpoint = {}

timeTrialCheckpoint.name = "Head2Head/TimeTrialCheckpoint"
timeTrialCheckpoint.depth = 0
timeTrialCheckpoint.color = {0.8, 0.4, 0.4, 0.8}
timeTrialCheckpoint.canResize = {true, true}
timeTrialCheckpoint.placements = {
    name = "default",
    data = {
        isStart = false,
        isFinish = false,
        checkpointNumber = -1,
    }
}

function timeTrialCheckpoint.rectangle(room, entity)
    return utils.rectangle(entity.x, entity.y, entity.width, entity.height)
end

return timeTrialCheckpoint