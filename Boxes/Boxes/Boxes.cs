using System;

using Addon;

namespace Addon
{
    public class Boxes : CPlugin // Always use : CPlugin behind your class name
    {
        public override ChatType OnSay(String Message, ServerClient Client, bool Teamchat)
        {
            if (Message == "!noclip") // This will let you fly through walls
            {
                Client.Other.NoClip = !Client.Other.NoClip; // Smart code from koro35
                return ChatType.ChatNone;

            }
            if (Message == "!pos") // This will let you fly through walls
            {
                ServerPrint(Client.Name + "is on the position X: " + Client.OriginX + " Y: " + Client.OriginY + " Z: " + Client.OriginZ); // Serverprints the coords
                TellClient(Client.ClientNum, "You are on the position X: " + Client.OriginX + " Y: " + Client.OriginY + " Z: " + Client.OriginZ, true); // Send a message to client
                return ChatType.ChatNone;
            }
            return ChatType.ChatContinue;
        } // This } will end the event

        public override void OnMapChange() // The event OnMapChange
        {
            string map = GetDvar("mapname");
            if (map == "mp_dome") // Spawn a solid box on Dome
            {
                Entity dome = SpawnModel("script_model", "com_plasticcase_trap_friendly", new Vector(-435f, 209f, -354f)); //Creates an entity named domeleft and spawns a model on the coordinates 462f, 108f, -220f
                Extensions.CloneBrushModelToScriptModel(dome, Extensions.FindAirdropCrateCollisionId()); // Makes the entity domeleft solid
            }
        } // This will end the event
    }
}
