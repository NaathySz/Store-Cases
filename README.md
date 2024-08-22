# [Store module] Cases
Set up and customize cases with different items.
# Features
- Chat and Html menu for buying cases
- Quick buy command (e.g `!quickbuycase <case index>` or `!quickbuycase "<case name>"`
- Configurable cooldown for buying cases
- Option to configure actions for opening owned items
- Option to toggle display of reward chances
- More
# Config
Config will be auto generated. Default/Example:
```json
{
  "Store_Cases_commands": [
    "storecases",
    "buycase"
  ],
  "Store_Quick_Buy_Cases_commands": [
    "quickbuycase"
  ],
  "Store_Give_Cases_commands": [
    "givecase"
  ],
  "Store_Give_Cases_permission": "@css/root", // Only players with the specified flag can give cases
  "Buy_Cases_Cooldown": 10, // Cooldown in seconds between case purchases
  "Menu_Type": "html", // Type of menu to display. Options are "html" or "chat"
  "Item_Action_If_Owned": 1, // 0 = Nothing | 1 = Reward with credits
  "Credit_Percentage": 50, // Percentage of case price to be rewarded as credits
  "Cases": [
    {
      "Name": "Standard", // Name of the case as shown in the main menu
      "Price": 500, // Price of the case in credits
      "Flag": "@css/vip", // Optional field. Only players with this permission can buy the case. Delete this line if you want everyone to have access
      "Rewards": [
        {
          "Type": "credits", // Type of reward. Should match the types defined in Store (or credits)
          "Value": "1000", // Amount of credits or identifier for other types of rewards
          "Description": "1000 Credits", // Description of the reward shown in Case Contents and final reward display
          "Chance": 90, // Probability percentage of this reward being selected
          "PrintToChatAll": false // If true, the reward announcement will be sent to all players when any player receives it
        },
        {
          "Type": "playerskin",
          "Value": "characters/models/nozb1/2b_nier_automata_player_model/2b_nier_player_model.vmdl",
          "Description": "2B Player Model",
          "Expiration": 172800, // Expiration time for the model in minutes. 0 = Never expires
          "Chance": 10,
          "PrintToChatAll": true
        }
      ]
    },
    {
      "Name": "Example",
      "Price": 1000,
      "Flag": null,
      "Rewards": [
        {
          "Type": "credits",
          "Value": "3000",
          "Description": "3000 Credits",
          "Expiration": 0,
          "Chance": 50,
          "PrintToChatAll": false
        },
        {
          "Type": "playerskin",
          "Value": "characters/models/ctm_diver/ctm_diver_variantb.vmdl",
          "Description": "Fernandez Frogman",
          "Expiration": 86400,
          "Chance": 20,
          "PrintToChatAll": false
        },
        {
          "Type": "playerskin",
          "Value": "characters/models/tm_professional/tm_professional_vari.vmdl",
          "Description": "Number K",
          "Expiration": 0,
          "Chance": 15,
          "PrintToChatAll": true
        },
        {
          "Type": "playerskin",
          "Value": "characters/models/ctm_fbi/ctm_fbi_variantf.vmdl",
          "Description": "Operator",
          "Expiration": 172800,
          "Chance": 15,
          "PrintToChatAll": true
        }
      ]
    }
  ],
  "Animation_Duration": 3, // Duration of the case animation in seconds
  "Animation_Interval": 0.1, // Interval between displaying each reward during animation (recommend avoiding very low values like 0.01; if Use_Html is true, avoid high values as animation may not display correctly)
  "Animation_Html": "Rolling...<br><font color='#FF0000'>{reward}</font>", // HTML code for displaying rewards during animation; used only if Use_Html is true
  "Reward_Html": "You won:<br><font color='#00FF00'>{reward}</font>", // HTML code for displaying the final reward; used only if Use_Html is true
  "Use_Html": false, // If true, it will use PrintToCenterHtml to display animations and rewards in HTML format (can be styled but may look awful due to the flashing effect). If false, it will use PrintToCenter.
  "Show_Reward_Chances": true, // If true, displays the chances of each reward in the Case Contents
  "ConfigVersion": 1
}
```
