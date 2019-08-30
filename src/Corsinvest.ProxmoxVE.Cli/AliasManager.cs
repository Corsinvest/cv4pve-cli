using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Corsinvest.ProxmoxVE.Cli
{
    /// <summary>
    /// Api alias
    /// </summary>
    public class AliasManager
    {
        private List<AliasDef> _alias = new List<AliasDef>()
        {
            //cluster
            new AliasDef("cluster-top,ct,top","Cluster top","get /cluster/resources",true),
            new AliasDef("cluster-top-node,ctn,topn","Cluster top for node","get /cluster/resources type:node",true),
            new AliasDef("cluster-top-storage,cts,tops","Cluster top for storage","get /cluster/resources type:storage",true),
            new AliasDef("cluster-top-vm,ctv,topv","Cluster top for VM/CT","get /cluster/resources type:vm",true),
            new AliasDef("cluster-status,csts","Cluster status","get /cluster/ha/status/current",true),
            new AliasDef("cluster-replication,crep","Cluster replication","get /cluster/replication",true),
            new AliasDef("cluster-backup,cbck","Cluster list vzdump backup schedule","get /cluster/backup",true),
            new AliasDef("cluster-backup-info,cbckinf","Cluster info backup schedule","get /cluster/backup/{backup}",true),

            //node
            new AliasDef("nodes-list,nlst","Node services","get /nodes",true),
            new AliasDef("node-status,nsts","Node status","get /nodes/{node}/status",true),
            new AliasDef("node-services,nsvc","Node services","get /nodes/{node}/services",true),
            new AliasDef("node-tasks-active,ntact","Node tasks active","get /nodes/{node}/tasks source:active",true),
            new AliasDef("node-tasks-error,nterr","Node tasks errors","get /nodes/{node}/tasks errors:1",true),
            new AliasDef("node-disks-list,ndlst","Node discks list","get /nodes/{node}/disks/list",true),
            new AliasDef("node-version,nver","Node version","get /nodes/{node}/version",true),
            new AliasDef("node-storage,nsto","Node storage info","get /nodes/{node}/storage",true),
            new AliasDef("node-storage-content,nstoc","Node storage content","get /nodes/{node}/storage/{storage}/content",true),
            new AliasDef("node-report,nrpt","Node report","get /nodes/{node}/report",true),
            new AliasDef("node-shutdown,nreb","Node reboot or shutdown","create /nodes/{node}/status command:cmd",true),
            new AliasDef("node-vzdump-config,nvcfg",
                         "Node Extract configuration from vzdump backup archive",
                         "get /nodes/{node}/vzdump/extractconfig volume:{volume}",
                         true),

            //Qemu
            new AliasDef("qemu-list,qlst","Qemu list vm","get /nodes/{node}/qemu",true),
            new AliasDef("qemu-exec,qexe","Qemu exec command vm","create /nodes/{node}/qemu/{vmid}/agent/exec command:{command}",true),
            new AliasDef("qemu-migrate,qmig","Qemu migrate vm other node"," get /nodes/{node}/qemu/{vmid}/migrate target:{target}",true),
            new AliasDef("qemu-vzdump-restore,qvrst","Qemu restore vzdump"," create /nodes/{node}/qemu vmid:{vmid} archive:{archive}",true),

            //status
            new AliasDef("qemu-status,qsts","Qemu current status vm","get /nodes/{node}/qemu/{vmid}/status/current",true),
            new AliasDef("qemu-start,qstr","Qemu start vm","create /nodes/{node}/qemu/{vmid}/status/start",true),
            new AliasDef("qemu-stop,qsto","Qemu stop vm","create /nodes/{node}/qemu/{vmid}/status/stop",true),
            new AliasDef("qemu-shutdown,qsdwn","Qemu shutdown vm","create /nodes/{node}/qemu/{vmid}/status/shutdown",true),
            new AliasDef("qemu-config,qcfg","Qemu config vm","get /nodes/{node}/qemu/{vmid}/config",true),

            //snapshot
            new AliasDef("qemu-snap-list,qslst","Qemu snapshot vm list","get /nodes/{node}/qemu/{vmid}/snapshot",true),
            new AliasDef("qemu-snap-create,qscrt","Qemu snapshot vm create","create /nodes/{node}/qemu/{vmid}/snapshot snapname:{snapname} description:{description}",true),
            new AliasDef("qemu-snap-delete,qsdel","Qemu snapshot vm delete","delete /nodes/{node}/qemu/{vmid}/snapshot/{snapname}",true),
            new AliasDef("qemu-snap-config,qscfg","Qemu snapshot vm delete","get /nodes/{node}/qemu/{vmid}/snapshot/{snapname}/config",true),
            new AliasDef("qemu-snap-rollback,qsrbck","Qemu snapshot vm rollback","create /nodes/{node}/qemu/{vmid}/snapshot/{snapname}/rollback",true),

            //LXC
            new AliasDef("lcx-list,llst","LXC list vm","get /nodes/{node}/lcx",true),
            new AliasDef("lxc-migrate,lmig","LXC migrate vm other node"," get /nodes/{node}/lxc/{vmid}/migrate target:{target}",true),
            new AliasDef("lxc-vzdump-restore,lvrst","LXC restore vzdump","create /nodes/{node}/lxc vmid:{vmid} ostemplate:{archive}",true),

            //status
            new AliasDef("lcx-status,lsts","LXC current status vm","get /nodes/{node}/lcx/{vmid}/status/current",true),
            new AliasDef("lcx-start,lstr","LXC start vm","create /nodes/{node}/lcx/{vmid}/status/start",true),
            new AliasDef("lcx-stop,lsto","LXC stop vm","create /nodes/{node}/lcx/{vmid}/status/stop",true),
            new AliasDef("lxc-shutdown,lsdwn","LXC shutdown vm","create /nodes/{node}/lxc/{vmid}/status/shutdown",true),
            new AliasDef("lcx-config,lcfg","LXC config vm","get /nodes/{node}/lcx/{vmid}/config",true),

            //snapshot
            new AliasDef("lcx-snap-list,lslst","LXC snapshot vm list","get /nodes/{node}/lcx/{vmid}/snapshot",true),
            new AliasDef("lcx-snap-create,lscrt","LXC snapshot vm create","create /nodes/{node}/lcx/{vmid}/snapshot snapname:{snapname} description:{description}",true),
            new AliasDef("lcx-snap-delete,lsdel","LXC snapshot vm delete","delete /nodes/{node}/lcx/{vmid}/snapshot/{snapname}",true),
            new AliasDef("lcx-snap-config,lscfg","LXC snapshot vm delete","get /nodes/{node}/lcx/{vmid}/snapshot/{snapname}/config",true),
            new AliasDef("lxc-snap-rollback,lsrbck","LXC snapshot vm rollback","create /nodes/{node}/lxc/{vmid}/snapshot/{snapname}/rollback",true),
        };

        /// <summary>
        /// Alias
        /// </summary>
        /// <returns></returns>
        public ReadOnlyCollection<AliasDef> Data => _alias.AsReadOnly();

        /// <summary>
        /// Create new alias
        /// </summary>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="command"></param>
        /// <param name="system"></param>
        /// <returns></returns>
        public bool Create(string name, string description, string command, bool system)
        {
            if (!AliasDef.IsValid(name) || Exists(name)) { return false; }
            _alias.Add(new AliasDef(name, description, command, system));
            return true;
        }

        /// <summary>
        /// Exists element
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Exists(string name) => _alias.Any(a => a.Exists(name));

        /// <summary>
        /// Remove alias
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Remove(string name)
        {
            var item = _alias.Where(a => a.Names.Contains(name) && !a.System).FirstOrDefault();
            if (item != null) { _alias.Remove(item); }
            return item != null;
        }

        /// <summary>
        /// Filename
        /// </summary>
        /// <value></value>
        public string FileName { get; set; }

        /// <summary>
        /// Load from file
        /// </summary>
        public void Load()
        {
            if (!File.Exists(FileName)) { File.WriteAllLines(FileName, new string[] { }); }

            foreach (var line in File.ReadAllLines(FileName))
            {
                var data = line.Split('\t');
                if (data.Length == 3) { Create(data[0], data[1], data[2], false); }
            }
        }

        /// <summary>
        /// Save to file.
        /// </summary>
        public void Save()
            => File.WriteAllLines(FileName, _alias.Where(a => !a.System)
                                                  .Select(a => $"{a.Name}\t{a.Description}\t{a.Command}"));
    }
}