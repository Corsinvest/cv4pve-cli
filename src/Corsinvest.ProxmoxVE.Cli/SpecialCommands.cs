/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.CommandLine;
using Corsinvest.ProxmoxVE.Api.Console.Helpers;

namespace Corsinvest.ProxmoxVE.Cli;

internal class SpecialCommands
{
    private const string Castle = @"
                                  .-.
                                 /___\
                                 |___|
                                 |]_[|
                                 / I \
                              JL/  |  \JL
   .-.                    i   ()   |   ()   i                    .-.
   |_|     .^.           /_\  LJ=======LJ  /_\           .^.     |_|
._/___\._./___\_._._._._.L_J_/.-.     .-.\_L_J._._._._._/___\._./___\._._._
       ., |-,-| .,       L_J  |_| [I] |_|  L_J       ., |-,-| .,        .,
       JL |-O-| JL       L_J%%%%%%%%%%%%%%%L_J       JL |-O-| JL        JL
IIIIII_HH_'-'-'_HH_IIIIII|_|=======H=======|_|IIIIII_HH_'-'-'_HH_IIIIII_HH_
-------[]-------[]-------[_]----\.=I=./----[_]-------[]-------[]--------[]-
 _/\_  ||\\_I_//||  _/\_ [_] []_/_L_J_\_[] [_] _/\_  ||\\_I_//||  _/\_  ||\
 |__|  ||=/_|_\=||  |__|_|_|   _L_L_J_J_   |_|_|__|  ||=/_|_\=||  |__|  ||-
 |__|  |||__|__|||  |__[___]__--__===__--__[___]__|  |||__|__|||  |__|  |||
IIIIIII[_]IIIII[_]IIIIIL___J__II__|_|__II__L___JIIIII[_]IIIII[_]IIIIIIII[_]
 \_I_/ [_]\_I_/[_] \_I_[_]\II/[]\_\I/_/[]\II/[_]\_I_/ [_]\_I_/[_] \_I_/ [_]
./   \.L_J/   \L_J./   L_JI  I[]/     \[]I  IL_J    \.L_J/   \L_J./   \.L_J
|     |L_J|   |L_J|    L_J|  |[]|     |[]|  |L_J     |L_J|   |L_J|     |L_J
|_____JL_JL___JL_JL____|-||  |[]|     |[]|  ||-|_____JL_JL___JL_JL_____JL_J
";

    private const string Cow = @"
 Welcome to cv4pve-cli!
        \   ^__^
         \  (oo)\_______
            (__)\       )\/\
                ||----w |
                ||     ||
    ";

    // 4. Panoramic Mountains
    private const string Mountains = @"
                                                _
                  ___                          (_)
                _/XXX\
 _             /XXXXXX\_                                    __
 X\__    __   /X XXXX XX\                          _       /XX\__      ___
     \__/  \_/__       \ \                       _/X\__   /XX XXX\____/XXX\
   \  ___   \/  \_      \ \               __   _/      \_/  _/  -   __  -  \__/
  ___/   \__/   \ \__     \\__           /  \_//  _ _ \  \     __  /  \____//
 /  __    \  /     \ \_   _//_\___     _/    //           \___/  \/     __/
 __/_______\________\__\_/________\_ _/_____/_____________/_______\____/_______
                                   /|\
                                  / | \
                                 /  |  \
                                /   |   \
                               /    |    \
                              /     |     \
                             /      |      \
                            /       |       \
                           /        |        \
                          /         |         \
";


    public static void AddCommands(RootCommand command)
    {
        var mountains = command.AddCommand("show-mountains", "Display a mountain landscape");
        mountains.SetAction((_) => Console.Out.WriteLine(Mountains));
        mountains.Hidden = true;

        var cow = command.AddCommand("show-cow", "Display a secret cow");
        cow.SetAction((_) => Console.Out.WriteLine(Cow));
        cow.Hidden = true;

        var castle = command.AddCommand("show-castle", "Display a secret castle");
        castle.SetAction((_) => Console.Out.WriteLine(Castle));
        castle.Hidden = true;
    }
}
