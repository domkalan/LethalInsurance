using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using NetworkManager = Unity.Netcode.NetworkManager;
using LethalTerminalExtender.Patches;

/**
 * A bit of a prewarning before digging into this source code.
 *
 * This was my first mod for a game ever. This started out as a proof of concept and never really got cleaned up.
 *
 * You will notice the use of a static class which runs the logic on the server side. Then the use of a NetworkBehavior which I added later for multiplayer.
 * This is not the ideal way to do this, sorry.
 *
 * You will also notice there are more insurance types to be added later.
 */
namespace LethalInsurance.Patches
{
    [Serializable]
    public struct LethalInsuranceCoverage : INetworkSerializable
    {
        public bool quotaCoverage;
        public bool scrapCoverage;
        public bool employeeCoverage;
        public bool equipmentCoverage;

        public LethalInsuranceCoverage()
        {
            quotaCoverage = false;
            scrapCoverage = false;
            employeeCoverage = false;
            equipmentCoverage = false;
        }
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref quotaCoverage);
            serializer.SerializeValue(ref scrapCoverage);
            serializer.SerializeValue(ref employeeCoverage);
            serializer.SerializeValue(ref equipmentCoverage);
        }
    }
    
    public static class LethalInsurance
    {
        private static TerminalCustomCommand rootCommand;
        
        private static string confirmText = "\n\nPlease CONFIRM or DENY.\n";

        private static GameObject networkObject;

        public static LethalInsuranceCoverage currentCoverage;

        public static Terminal terminalInstace;

        public static Action<bool, string> coverageServerRetrun;
        
        public static void Register()
        {
            currentCoverage = new LethalInsuranceCoverage();
            
            rootCommand = TerminalExtenderUtils.addQuickCommand("insurance", "Insurance offers coverage plans to give employees peace of mind.\n\n" +
                ">QUOTA\nReimbursement for when you return to the company but are short on quota.\n\n" +
                //">SCRAP\nReplaces lost scrap in the event autopilot gets a little impatient.\n\n" +
                //">EMPLOYEE\nCovers damaged equipment when an employee goes missing.\n\n" +
                //">MORE\nView additional coverage plans.\n\n" +
                ">QUIT\nQuits the insurance application\n\n", true, (Terminal term, TerminalNode node) =>
            {
                if (terminalInstace == null)
                    terminalInstace = term;
                
                listenForMenuInput();
            });
        }

        private static void parseCommand(string command)
        {
            switch (command)
                    {
                        case "quota":
                            promptQuotaInsurance();
                            
                            break;
                        case "scrap":
                            promptScrapInsurance();
                            
                            break;
                        case "employee":
                            promptEmployeeInsurance();
                            
                            break;
                        case "equipment":
                            promptEquipmentInsurance();
                            
                            break;
                        case "help":
                            TerminalExtenderUtils.displayNodeOnTerminal(rootCommand.node);
                            
                            listenForMenuInput();
                            
                            break;
                        case "more":
                            TerminalExtenderUtils.writeToTerminal("Additional Coverage\n\n" +
                                                                  ">EQUIPMENT\nReplaces any equipment that an employee \"accidentally\" left behind at a job site.\n\n" +
                                                                  ">COVERAGE\nView current coverage information.\n\n" +
                                                                  ">HELP\nShows the basic information how to use the insurance application.\n\n", true);
                            
                            listenForMenuInput();
                            
                            break;
                        case "quit":
                            TerminalExtenderUtils.releaseOnSubmit();
                            
                            TerminalExtenderUtils.cleanTerminalScreen();
                            
                            TerminalExtenderUtils.setInputState(true);
                            
                            TerminalExtenderUtils.writeToTerminal("Goodbye.\n\n");
                            
                            break;
                        default:
                            TerminalExtenderUtils.writeToTerminal("Invalid command, please use HELP for more info.\n", true);
                            
                            TerminalExtenderUtils.setInputState(true);
                            
                            TerminalExtenderUtils.playAudioClip(1);
                            
                            break;
                    }
        }

        private static void listenForMenuInput()
        {
            TerminalExtenderUtils.setInputState(true);
            
            TerminalExtenderUtils.setOnSubmit((string command) =>
            {
                parseCommand(command);
            });
        }

        private static void displayConfirmOrDeny(Action callback)
        {
            TerminalExtenderUtils.setInputState(true);
            
            TerminalExtenderUtils.setOnSubmit((string command) =>
            {
                if (!(command == "c" || command == "confirm" || command == "conf" || command == "buy" || command == "yes" || command == "true"))
                {
                    TerminalExtenderUtils.writeToTerminal("Cancelled order.\n");
                    
                    listenForMenuInput();
                    
                    return;
                }
                
                callback();
            });
        }

        private static void purchaseOnServer(int coveragePlan)
        {
            coverageServerRetrun = (success, message) =>
            {
                if (!success)
                {
                    TerminalExtenderUtils.playAudioClip(1);
                }
                
                TerminalExtenderUtils.writeToTerminal(message);

                coverageServerRetrun = null;
            };
            
            LethalInsurance_NetworkUtil.Instance.purchaseCoverageServerRPC(0);
        }

        private static void promptQuotaInsurance()
        {
            double quotaCoverageCost = getQuotaCoverageCost();
            
            TerminalExtenderUtils.writeToTerminal("Quota Coverage\nReimbursement for when you return to the company but are short on quota.\n\n" +
                                                  "TERMS:\n* Reimbursement can only be paid out on day 0 at the company headquarters.\n" +
                                                  "* Coverage is single use but can be purchased again.\n" +
                                                  "* Must have met at least 70% of quota to qualify for reimbursement.\n" +
                                                  "* Coverage must be bought before day 1.\n" +
                                                  "* Coverage is not refundable.\n\n" +
                                                  "Total cost of coverage: $" + quotaCoverageCost + "." + confirmText, true);
            
            displayConfirmOrDeny(() =>
            {
                purchaseOnServer(0);
            });
        }
        
        private static void promptScrapInsurance()
        {
            TerminalExtenderUtils.writeToTerminal("Scrap Coverage\nReplaces all lost scrap value in the event all employees do not return to the ship.\n\n" +
                                                  "TERMS:\n* Coverage is single use and will need to be purchased again afterwards.\n" +
                                                  "* Coverage is not refundable.\n\n" +
                                                  "Total cost of coverage: $800." + confirmText, true);
            
            displayConfirmOrDeny(() =>
            {
                TerminalExtenderUtils.playAudioClip(0);
                
                TerminalExtenderUtils.writeToTerminal("Scrap coverage has been purchased!\n");
                
                listenForMenuInput();
            });
        }
        
        private static void promptEmployeeInsurance()
        {
            TerminalExtenderUtils.writeToTerminal("Employee Coverage\nReplaces damaged equipment in the event an employee becomes worm food or equivalent.\n\n" +
                                                  "TERMS:\n* Coverage will last 4 cycles and can be purchased again afterwards.\n" +
                                                  "* Covered equipment is limited to:\n" +
                                                  "    * Walkie Talkies\n" +
                                                  "    * Flashlights\n" +
                                                  "    * Jetpacks\n" +
                                                  "    * Shotguns\n" +
                                                  "* Employee body must be 100% non recoverable." +
                                                  "* Coverage is not refundable.\n\n" + 
                                                  "Total cost of coverage: $800."+ confirmText, true);
            
            displayConfirmOrDeny(() =>
            {
                TerminalExtenderUtils.playAudioClip(0);
                
                TerminalExtenderUtils.writeToTerminal("Employee coverage has been purchased!\n");
                
                listenForMenuInput();
            });
        }
        
        private static void promptEquipmentInsurance()
        {
            TerminalExtenderUtils.writeToTerminal("Equipment Coverage\nReplaces equipment that is left behind at the job site.\n\n" +
                                                  "TERMS:\n* Covered equipment is limited to:\n" +
                                                  "    * Ladders\n" +
                                                  "    * Radars\n" +
                                                  "    * Shotguns\n" +
                                                  "* Coverage is not refundable.\n\n" + 
                                                  "Total cost of coverage: $800." + confirmText, true);
            
            displayConfirmOrDeny(() =>
            {
                TerminalExtenderUtils.playAudioClip(0);
                
                TerminalExtenderUtils.writeToTerminal("Equipment coverage has been purchased!\n");
                
                listenForMenuInput();
            });
        }

        public static int getQuotaCoverageCost()
        {
            int quotaCost = TimeOfDay.Instance.profitQuota;
            double quotaCoverageCost = Math.Floor((quotaCost * 0.20f));

            return Convert.ToInt32(quotaCoverageCost);
        }

        public static bool chargeTerminalBalance(int balance)
        {
            if (terminalInstace == null)
            {
                Terminal term = GameObject.FindObjectOfType<Terminal>();

                if (term == null)
                {
                    throw new Exception("Could not find terminal object!");
                }
                
                terminalInstace = term;
            }

            if (terminalInstace.groupCredits < balance)
            {
                return false;
            }

            terminalInstace.groupCredits -= balance;
            
            terminalInstace.SyncGroupCreditsClientRpc(terminalInstace.groupCredits, terminalInstace.numberOfItemsInDropship);
            terminalInstace.PlayTerminalAudioClientRpc(0);

            return true;
        }

        public static void handleNetworkManagerFetch(NetworkManager netMan)
        {
            // only allow this to run once
            if (networkObject != null)
                return;
            
            Plugin.instance.Log(LogType.Log, "HOOKED TO NETWORK MANAGER");
            
            AssetBundle assetBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "lethalinsurance"));
            GameObject networkPrefab = (GameObject)assetBundle.LoadAsset("LethalInsuranceNetwork");
            networkPrefab.AddComponent<LethalInsurance_NetworkUtil>();
            
            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);

            networkObject = networkPrefab;
        }

        public static void handleGameRoundStart()
        {
            Plugin.instance.Log(LogType.Log, "HOOKED TO GAME ROUND START");
            
            if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
            {
                GameObject networkHandlerHost = GameObject.Instantiate(networkObject, Vector3.zero, Quaternion.identity);
                networkHandlerHost.GetComponent<NetworkObject>().Spawn();
            }
        }

        public static void handleClientPurcahseReturn(bool status, string message)
        {
            if (coverageServerRetrun == null)
                return;

            coverageServerRetrun(status, message);
        }

        public static void handleClientPurchaseCoverage(ulong clientId, int coverage)
        {
            int daysLeft = TimeOfDay.Instance.daysUntilDeadline;
            
            switch (coverage)
            {
                case 0:
                    if (daysLeft < 2)
                    {
                        returnClientPurchase(clientId, false, "Quota coverage must be purchased before day 1.");
                        
                        return;
                    }

                    int quotaCoverageCost = getQuotaCoverageCost();

                    bool chargeSuccess = chargeTerminalBalance(quotaCoverageCost);

                    if (!chargeSuccess)
                    {
                        returnClientPurchase(clientId, false, "You do not have enough credits for quota coverage.");

                        return;
                    }

                    currentCoverage.quotaCoverage = true;
                    
                    returnClientPurchase(clientId, true, "Quota coverage has been purchased.");
                    
                    LethalInsurance_NetworkUtil.Instance.showMessageClientRPC("Quota insurance has been purchased!", false);
                    
                    break;
                case 1:
                    break;
                case 2:
                    break;
                case 3:
                    break;
                default:
                    break;
            }
            
            LethalInsurance_NetworkUtil.Instance.syncCoverageClientRPC(currentCoverage);
        }

        public static void returnClientPurchase(ulong clientId, bool purchaseResult, string message = "")
        {
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[]{clientId}
                }
            };
            
            LethalInsurance_NetworkUtil.Instance.purchaseResultClientRPC(purchaseResult, message, clientRpcParams);
        }
        
        
        public static void handleFired()
        {
            Plugin.instance.Log(LogType.Log, "All insurance coverages have been reset");

            currentCoverage = new LethalInsuranceCoverage();
            
            LethalInsurance_NetworkUtil.Instance.syncCoverageClientRPC(currentCoverage);
        }

        /**
         * This runs on the server, since we are hooking into the EndGameServerRPC
         */
        public static void handleEndOfRoundEvent()
        {
            Plugin.instance.Log(LogType.Log, "END OF ROUND EVENT FIRED");
            
            int daysLeft = TimeOfDay.Instance.daysUntilDeadline;
            string levelName = StartOfRound.Instance.currentLevel.sceneName;

            Plugin.instance.Log(LogType.Log, "LevelName: " + levelName + ", DaysLeft: " + daysLeft);
            
            if (daysLeft <= 0 && levelName == "CompanyBuilding")
            {
                int quotaTarget = TimeOfDay.Instance.profitQuota;
                int quotaGathered = TimeOfDay.Instance.quotaFulfilled;
                
                Plugin.instance.Log(LogType.Log, "quotaTarget: " + quotaTarget + ", QuotaGathered: " + quotaGathered);

                bool shortOnQuota = quotaGathered < quotaTarget;
                
                Plugin.instance.Log(LogType.Log, "ShortOnQuota: " + shortOnQuota + ", insurancePurchased: " + currentCoverage.quotaCoverage);

                if (shortOnQuota && !currentCoverage.quotaCoverage)
                {
                    // Since we are already here, we can handle the firing event
                    handleFired();
                    
                    return;
                }

                double quotaShortPercent = ((double)quotaGathered / (double)quotaTarget) * 100;
                
                Plugin.instance.Log(LogType.Log, "ShortOnQuotaPercent: " + quotaShortPercent);

                if (quotaShortPercent < 70)
                {
                    LethalInsurance_NetworkUtil.Instance.showMessageClientRPC("You did not meet the 70% minimum for quota coverage. You were short by " + quotaShortPercent, true);
                    
                    // Since we are already here we can handle the firing event
                    handleFired();
                    
                    return;
                }
                
                // Inspired by https://github.com/tinyhoot/ShipLoot/blob/main/ShipLoot/Patches/HudManagerPatcher.cs
                GameObject companyShip = GameObject.Find("/Environment/HangarShip");
                List<GrabbableObject> lootOnShip = companyShip.GetComponentsInChildren<GrabbableObject>().Where(item => item.itemProperties.isScrap && !item.itemUsedUp).ToList();
                
                int scrapValue = lootOnShip.Sum(scrap => scrap.scrapValue);
                
                Plugin.instance.Log(LogType.Log, "ShipScrapValue: " + scrapValue + ", quotaMet: " + quotaGathered + ", quotaTarget: " + quotaTarget);

                if (shortOnQuota && scrapValue <= 0)
                {
                    int shortOnQuotaBy = (quotaTarget - quotaGathered) + 10;
                    StartOfRound.Instance.gameStats.scrapValueCollected += shortOnQuotaBy;
                    TimeOfDay.Instance.quotaFulfilled += shortOnQuotaBy;

                    currentCoverage.quotaCoverage = false;
                    
                    LethalInsurance_NetworkUtil.Instance.showMessageClientRPC("Insurance coverage for missed quota has paid out to the company.", true);
                }
            }
        }

        public static void handleEndGameLevelPulled()
        {
            if (currentCoverage.quotaCoverage)
            {
                int daysLeft = TimeOfDay.Instance.daysUntilDeadline;
                string levelName = StartOfRound.Instance.currentLevel.sceneName;

                if (daysLeft <= 0 && levelName == "CompanyBuilding")
                {
                    int quotaTarget = TimeOfDay.Instance.profitQuota;
                    int quotaGathered = TimeOfDay.Instance.quotaFulfilled;
                    double quotaShortPercent = ((double) quotaGathered / (double) quotaTarget) * 100;

                    if (quotaShortPercent < 70)
                    {
                        HUDManager.Instance.DisplayTip("Lethal Insurance", "Insurance will not payout, you have not met the 70% requirement.");
                    }
                } else if (daysLeft <= 0 && levelName != "CompanyBuilding")
                {
                    HUDManager.Instance.DisplayTip("Lethal Insurance", "Insurance will not payout, you are not at the company building.");
                }
            }
        }
    }

    /**
     * Our main network handler class, this will communicate stuff over the network for us
     */
    // A big thanks to https://lethal.wiki/advanced-modding/networking
    public class LethalInsurance_NetworkUtil : NetworkBehaviour
    {
        public static LethalInsurance_NetworkUtil Instance { get; private set; }

        // IDK why this is needed, but the wiki said so
        public override void OnNetworkSpawn()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                Instance?.gameObject.GetComponent<NetworkObject>().Despawn();
            
            Instance = this;

            base.OnNetworkSpawn();
        }

        [ServerRpc(RequireOwnership = false)]
        public void purchaseCoverageServerRPC(int coverage, ServerRpcParams serverRpcParams = default)
        {
            var clientId = serverRpcParams.Receive.SenderClientId;
            if (NetworkManager.ConnectedClients.ContainsKey(clientId))
            {
                // Forward message to the static class running on our server
                LethalInsurance.handleClientPurchaseCoverage(clientId, coverage);
            }
            
        }

        [ClientRpc]
        public void syncCoverageClientRPC(LethalInsuranceCoverage coverage)
        {
            LethalInsurance.currentCoverage = coverage;
        }
        
        [ClientRpc]
        public void showMessageClientRPC(string message, bool isWarning)
        {
            HUDManager.Instance.DisplayTip("Lethal Insurance", message, isWarning);
        }

        [ClientRpc]
        public void showChatMessageClientRPC(string message)
        {
            HUDManager.Instance.AddChatMessage(message, "Insurance");
        }

        [ClientRpc]
        public void purchaseResultClientRPC(bool success, string message, ClientRpcParams clientRpcParams = default)
        {
            LethalInsurance.handleClientPurcahseReturn(success, message);
        }
    }

    /**
     * Main class for all of our patches
     */
    [HarmonyPatch]
    public class LethalInsurance_Patches
    {
        /**
         * NetworkHooking so we can register our custom network object and reference the network manager
         */
        [HarmonyPrefix, HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        static void RunNetworkHook(GameNetworkManager __instance)
        {
            NetworkManager netMan = __instance.gameObject.GetComponent<NetworkManager>();
            
            LethalInsurance.handleNetworkManagerFetch(netMan);
        }
        
        /**
         * Hook so we can see when the round has ended
         */
        [HarmonyPrefix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.EndGameServerRpc))]
        static void RunEndGameHook(StartOfRound __instance)
        {
            LethalInsurance.handleEndOfRoundEvent();
        }

        /**
         * Hook so when we know the game has started
         */
        [HarmonyPrefix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
        static void RunGameAwake(StartOfRound __instance)
        {
            LethalInsurance.handleGameRoundStart();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(StartMatchLever), nameof(StartMatchLever.EndGame))]
        static void PulledEndGameLevel()
        {
            LethalInsurance.handleEndGameLevelPulled();
        }
    }
}
