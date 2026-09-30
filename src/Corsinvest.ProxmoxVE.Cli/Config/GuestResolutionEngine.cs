/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;

namespace Corsinvest.ProxmoxVE.Cli.Config;

/// <summary>
/// Auto-detects guest resolution from alias command paths and resolves node/vmid/vmtype.
/// </summary>
internal static class GuestResolutionEngine
{
    internal enum GuestResolution { None, Qemu, Lxc, Any }

    internal const string ArgGuestLong = "--guest";
    internal const string ArgGuestShort = "-g";

    private const string SegNodes = "nodes";
    internal const string SegNode = "node";
    internal const string SegVmId = "vmid";
    internal const string SegVmType = "vmtype";
    internal const string TypeQemu = "qemu";
    internal const string TypeLxc = "lxc";

    private const string TagNode = "{" + SegNode + "}";
    private const string TagVmId = "{" + SegVmId + "}";
    private const string TagVmType = "{" + SegVmType + "}";

    private static readonly string[] TagsQemuLxc = [SegNode, SegVmId];
    private static readonly string[] TagsAny = [SegNode, SegVmType, SegVmId];

    /// <summary>
    /// Detects from the alias command path whether guest resolution is needed.
    /// Returns the type of resolution and the tags that will be auto-resolved (node, vmid, vmtype).
    /// </summary>
    internal static (GuestResolution Resolution, string[] ResolvedTags) Detect(string command)
    {
        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) { return (GuestResolution.None, []); }
        var segs = parts[1].ToLowerInvariant().Split('/', StringSplitOptions.RemoveEmptyEntries);

        for (var i = 0; i < segs.Length - 2; i++)
        {
            if (segs[i] != SegNodes || segs[i + 1] != TagNode) { continue; }
            if (i + 3 >= segs.Length) { break; }
            var vmtypeSeg = segs[i + 2];
            if (segs[i + 3] != TagVmId) { break; }

            if (vmtypeSeg == TypeQemu) { return (GuestResolution.Qemu, TagsQemuLxc); }
            if (vmtypeSeg == TypeLxc) { return (GuestResolution.Lxc, TagsQemuLxc); }
            if (vmtypeSeg == TagVmType) { return (GuestResolution.Any, TagsAny); }
            break;
        }
        return (GuestResolution.None, []);
    }

    private static IEnumerable<ClusterResource> Filter(IEnumerable<ClusterResource> items, GuestResolution resolution)
        => resolution switch
        {
            GuestResolution.Qemu => items.Where(a => a.VmType == VmType.Qemu),
            GuestResolution.Lxc => items.Where(a => a.VmType == VmType.Lxc),
            _ => items,
        };

    internal static IEnumerable<string> GetCompletions(GuestResolution resolution, PveClient? client)
    {
        if (client == null) { return []; }
        try
        {
            var items = client.Cluster.Resources.GetAsync(ClusterResourceType.Vm).GetAwaiter().GetResult()
                              .Where(a => !a.IsUnknown);
            var filtered = Filter(items, resolution).ToList();
            return filtered.Select(a => a.VmId.ToString())
                           .Concat(filtered.Select(a => a.Name).Where(n => !string.IsNullOrWhiteSpace(n)))
                           .Order();
        }
        catch { return []; }
    }

}
