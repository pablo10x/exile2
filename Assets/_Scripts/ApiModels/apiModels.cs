using System;

namespace core.ApiModels {
    
    
    
    [Serializable] public class APIAuthResponse {
        public bool accountexist;
        public string ws_auth_key;
    }

    [Serializable] public class APIAuthRequest {
        public string id_token;

    }

    [Serializable] public class APIPlayer {
        public long   id;
        public string uid;
        public string name;
        public int    xp;
        // Add other fields as needed
    }
}