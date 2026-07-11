using System;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;

namespace GFC_ComBadge
{
    /// <summary>
    /// Set up the mute and deafen keybinds within Discord using the Enter key and a 5-second delay
    /// </summary>
    internal static class Setup
    {
        private static readonly InputSimulator InputSim = new();

        public static async Task RunSetupAsync()
        {
            Console.Clear();
            Console.WriteLine("====================================================");
            Console.WriteLine("          COMBADGE FIRST START SETUP                ");
            Console.WriteLine("====================================================");
            Console.WriteLine();

            await ExecuteSetupStepAsync(
                stepNumber: 1,
                actionName: "MUTE",
                discordAction: "Toggle Mute",
                keyToSend: VirtualKeyCode.F24
            );

            await ExecuteSetupStepAsync(
                stepNumber: 2,
                actionName: "DEAFEN",
                discordAction: "Toggle Deafen",
                keyToSend: VirtualKeyCode.F23
            );

            ShowSuccessScreen();
            await Task.Delay(3000);
        }

        private static async Task ExecuteSetupStepAsync(int stepNumber, string actionName, string discordAction, VirtualKeyCode keyToSend)
        {
            bool isStepSuccessful = false;

            while (!isStepSuccessful)
            {
                Console.Clear();
                Console.WriteLine("====================================================");
                Console.WriteLine($"          STEP {stepNumber}: RECORD {actionName} KEYBIND             ");
                Console.WriteLine("====================================================");
                Console.WriteLine();

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Recording {actionName} keybind...");
                Console.ResetColor();

                Console.WriteLine($"1. Open Discord -> User Settings -> Keybinds.");
                Console.WriteLine($"2. Click 'Add a Keybind' and set Action to '{discordAction}'.");
                Console.WriteLine($"3. Press [ENTER] here. You will have 5 seconds to switch to Discord and click the keybind field.");
                Console.WriteLine();
                Console.WriteLine("--> Press [ENTER] when ready to start the 5s countdown...");

                WaitForEnter();

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("--> SWITCH TO DISCORD NOW AND CLICK THE KEYBIND FIELD! <--");
                Console.Write("Triggering in: ");

                for (int i = 5; i > 0; i--)
                {
                    Console.Write($"{i}... ");
                    await Task.Delay(1000);
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("NOW!");
                Console.ResetColor();

                InputSim.Keyboard.KeyPress(keyToSend);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n[✓] {actionName} signal sent ({keyToSend})!");
                Console.ResetColor();

                Console.Write("Has the keybind been successfully recorded in Discord? (Y/N): ");
                isStepSuccessful = AskYesNo();
            }
        }

        private static void WaitForEnter()
        {
            while (true)
            {
                var keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.Enter)
                    break;
            }
        }

        private static bool AskYesNo()
        {
            while (true)
            {
                var keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.Y)
                {
                    Console.WriteLine("Y");
                    return true;
                }
                if (keyInfo.Key == ConsoleKey.N)
                {
                    Console.WriteLine("N");
                    return false;
                }
            }
        }

        private static void ShowSuccessScreen()
        {
            Console.Clear();
            Console.WriteLine("====================================================");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("   SETUP COMPLETED SUCCESSFULLY!");
            Console.ResetColor();
            Console.WriteLine("   Your Combadge is now linked to Discord.");
            Console.WriteLine("   Configuration saved to AppData.");
            Console.WriteLine("====================================================");
            Console.WriteLine("\nStarting application in 3 seconds...");
        }
    }
}