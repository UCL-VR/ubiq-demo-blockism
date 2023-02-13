using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Avatars;
using Ubiq.Messaging;
using Ubiq.Rooms;
using Ubiq.Samples;
using UnityEngine;


public class RoleManager : MonoBehaviour
{
    private List<string> roles = new List<string> { "red", "yellow", "blue", "green" };

    private List<Color> role_colors = new List<Color> { Color.red, Color.yellow, Color.blue, Color.green };

    private NetworkContext context;

    private RoomClient room_client;

    private AvatarManager avatar_manager;

    // set of variables for messages
    private List<string> avatar_ids;
    private List<string> avatar_roles;
    private string master_peer_id;
    // set of variables for messages

    public NetworkId NetworkId => new NetworkId("a15ca05dbb9ef8ec");

    private string room_id;

    private static System.Random rng;

    public Texture2D avatarTextureTemplate;

    struct Message
    {
        public List<string> avatar_ids;
        public List<string> avatar_roles;
        public string master_peer_id;
        public string room_id;

        public Message(List<string> ai, List<string> ar, string mpi, string rid)
        {
            this.avatar_ids = ai;
            this.avatar_roles = ar;
            this.master_peer_id = mpi;
            this.room_id = rid;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        rng = new System.Random();

        context = NetworkScene.Register(this);

        room_client = context.Scene.GetComponentInChildren<RoomClient>();

        room_client.OnPeerAdded.AddListener(OnPeerAdded);

        room_client.OnJoinedRoom.AddListener(OnJoinedRoom);

        avatar_manager = AvatarManager.Find(this);

        if(avatar_manager.OnAvatarCreated == null)
        {
            avatar_manager.OnAvatarCreated = new AvatarManager.AvatarCreatedEvent();
        }
        avatar_manager.OnAvatarCreated.AddListener(OnAvatarCreated);
    }

    private struct AvatarTextureCache
    {
        public string role;
        public Texture2D texture;
    }

    private Dictionary<Ubiq.Avatars.Avatar, AvatarTextureCache> avatar_texture_cache = new Dictionary<Ubiq.Avatars.Avatar, AvatarTextureCache>();

    void OnAvatarCreated(Ubiq.Avatars.Avatar avatar)
    {
        // when a peer gets or changes its role, update the avatar texture

        if (!avatar_texture_cache.ContainsKey(avatar))
            avatar_texture_cache[avatar] = new AvatarTextureCache();

        var cache = avatar_texture_cache[avatar];
        var role = avatar.Peer["blockism.color"];

        if(cache.role != role)
        {
            // update the texture
            var texture_manager = avatar.GetComponent<TexturedAvatar>();
            var existing = texture_manager.GetTexture();

            if(existing == null)
            {
                return;
            }

            var tint = role_colors[roles.IndexOf(role)];

            cache.texture = new Texture2D(existing.width, existing.height);

            for (int x = 0; x < existing.width; x++)
            {
                for (int y = 0; y < existing.height; y++)
                {
                    var pixel = avatarTextureTemplate.GetPixel(x, y);
                    if (pixel.a > 0)
                    {
                        cache.texture.SetPixel(x, y, Color.Lerp(pixel, tint, 0.75f));
                    }
                    else
                    {
                        cache.texture.SetPixel(x, y, existing.GetPixel(x, y));
                    }
                }
            }
            cache.texture.Apply();

            foreach(var item in avatar.GetComponentsInChildren<MeshRenderer>())
            {
                item.material.mainTexture = cache.texture;
            }

            cache.role = role;
        }
    }

    void Update()
    {
        // check if master peer has left and pick a new one 

        if (room_client.Room.UUID == room_id && !string.IsNullOrEmpty(master_peer_id))
        {
            var avatars = avatar_manager.Avatars;
            bool found_master = false;

            foreach (var avatar in avatars)
            {
                if (avatar.Peer.uuid == master_peer_id)
                {
                    found_master = true;
                    break;
                }
            }

            if (!found_master)
            {
                RemoveAvatarAndRole(master_peer_id);

                // select self as master peer 
                master_peer_id = avatars.First().Peer.uuid;

                SendMessageUpdate();
            }
        }

        // check if the a peer has left and update lists if they have (only for master peer)
        if ((room_client.Room.UUID == room_id) && (room_client.Me.uuid == master_peer_id))
        {
            var avatars = avatar_manager.Avatars;

            var ids_to_be_removed = new List<string>();

            foreach (var id in avatar_ids)
            {
                bool id_found = false;

                foreach (var avatar in avatars)
                {
                    if (avatar.Peer.uuid == id)
                    {
                        id_found = true;
                        break;
                    }
                }

                if (!id_found)
                {
                    ids_to_be_removed.Add(id);
                }
            }

            // remove the ids collected in above loop
            ids_to_be_removed.ForEach((id) => RemoveAvatarAndRole(id));

            SendMessageUpdate();
        }
    }

    private void AddAvatarAndRole(string avatar_id, string color)
    {
        if (!avatar_ids.Contains(avatar_id))
        {
            avatar_ids.Add(avatar_id);
            avatar_roles.Add(color);
        }
        else
        {
            avatar_roles[avatar_ids.IndexOf(avatar_id)] = color;
        }
    }

    private void RemoveAvatarAndRole(string id)
    {
        int peer_index = avatar_ids.IndexOf(id);
        avatar_roles.RemoveAt(peer_index);
        avatar_ids.RemoveAt(peer_index);
    }

    // randomly shuffle the roles of the players 
    public void ShuffleRoles()
    {
        // only master peer can change roles and send message updates 
        if (room_client.Me.uuid != master_peer_id)
        {
            return;
        }

        // shuffle elements in avatar roles 
        avatar_roles.OrderBy(role => rng.Next()).ToList();

        // change prefab if at the client of an avatar
        foreach (var avatar in avatar_manager.Avatars)
        {
            OnAvatarCreated(avatar);
        }

        SendMessageUpdate();
    }

    private void SendMessageUpdate()
    {
        Message message;
        message.avatar_ids = avatar_ids;
        message.avatar_roles = avatar_roles;
        message.master_peer_id = master_peer_id;
        message.room_id = room_id;

        context.SendJson(message);
    }

    private void OnJoinedRoom(IRoom room)
    {
        // do not set master peer for empty room 
        if (string.IsNullOrEmpty(room.JoinCode)
                && string.IsNullOrEmpty(room.Name)
                && string.IsNullOrEmpty(room.UUID))
        {
            return;
        }

        // check if the room has a master peer set
        if (!string.IsNullOrEmpty(room["blockism.master"]))
        {
            return;
        }

        // the room has not had the master property set yet, so we must be the
        // first

        // set first avatar as master peer      
        master_peer_id = room_client.Me.uuid;

        var role = roles.First();
        room_client.Me["blockism.color"] = role;

        foreach (var item in avatar_manager.Avatars)
        {
            OnAvatarCreated(item);
        }

        avatar_ids = new List<string>();
        avatar_roles = new List<string>();

        avatar_ids.Add(room_client.Me.uuid);
        avatar_roles.Add(role);
        room_id = room.UUID;

        SendMessageUpdate();
    }

    private void OnPeerAdded(IPeer peer)
    {
        // Attach roles and modify dict only if it is master peer's room 
        if (room_client.Me.uuid != master_peer_id)
        {
            return;
        }

        Dictionary<string, int> role_count = new Dictionary<string, int>();

        // loop through roles and initiate Dict  
        roles.ForEach(role => role_count.Add(role, 0));

        foreach (var avatar in room_client.Peers)
        {
            var role = avatar["blockism.color"];

            // avatar already has a role 
            if (!string.IsNullOrEmpty(role))
            {
                // register the role with the Dict 
                role_count[role] += 1;

                // mainain an internal list of ids and roles 
                AddAvatarAndRole(avatar.uuid, role);
            }
        }

        // choose role with min count as current avatar's role 
        var new_role = role_count.Aggregate((l, r) => l.Value < r.Value ? l : r).Key;

        // update lists 
        AddAvatarAndRole(peer.uuid, new_role);

        SendMessageUpdate();
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<Message>();

        // utilize message update only if not master peer 
        if (msg.master_peer_id == room_client.Me.uuid)
        {
            return;
        }

        // unload message updates to client variables 
        avatar_ids = msg.avatar_ids;
        avatar_roles = msg.avatar_roles;
        master_peer_id = msg.master_peer_id;
        room_id = msg.room_id;

        // change prefab if at the client of an avatar
        foreach (var avatar in avatar_manager.Avatars)
        {
            OnAvatarCreated(avatar);
        }
    }

    public List<int> GetAvatarColourIndexes()
    {
        List<int> avatar_roles_indexs = new List<int>();
        foreach (string avatar_role in avatar_roles)
            avatar_roles_indexs.Add(roles.IndexOf(avatar_role));
        return avatar_roles_indexs;
    }

    public List<string> GetAvatarRoles()
    {
        return avatar_roles;
    }
}
