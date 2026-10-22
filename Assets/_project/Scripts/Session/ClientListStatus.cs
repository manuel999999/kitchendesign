using System.Collections.Generic;
using Melcher;

namespace KitchenDesign.Session
{
    public sealed class ClientListStatus
    {
        private int? _activeClientId;
        private int? _activeQuoteId;

        public List<UserClient> UserClients { get; set; } = new(capacity: 0);

        public bool? ActiveClientWantsToBeContacted { get; set; }

        public int? ActiveClientId
        {
            get => this._activeClientId;
            set
            {
                if (value != this._activeClientId)
                {
                    MelcherLog.LogEditorOnly("Active client ID is now: " + value);
                    this._activeClientId = value;
                }
            }
        }

        public int? ActiveQuoteId
        {
            get => this._activeQuoteId;
            set
            {
                if (value != this._activeQuoteId)
                {
                    MelcherLog.LogEditorOnly("Active quote ID is now: " + value);
                    this._activeQuoteId = value;
                }
            }
        }

        public void Initialize()
        {
            this.ActiveClientId = null;
            this.ActiveQuoteId = null;
            this.ActiveClientWantsToBeContacted = null;
            this.UserClients.Clear();
        }

        public bool TryGetClientAndQuoteIndex(int clientId, int quoteId, out int clientIndex, out int quoteIndex, bool logErrorIfNotFound = true)
        {
            quoteIndex = -1;

            return this.TryGetClientIndex(clientId, out clientIndex, logErrorIfNotFound) &&
                this.TryGetClientQuoteIndex(clientIndex, quoteId, out quoteIndex, logErrorIfNotFound);
        }

        public bool TryGetClientIndex(int clientId, out int clientIndex, bool logErrorIfNotFound = true)
        {
            clientIndex = this.UserClients.FindIndex(
                x => x.id == clientId
            );

            if (clientIndex < 0 && logErrorIfNotFound)
            {
                MelcherLog.LogError($"Did not find client with ID {clientId}");
            }

            return clientIndex >= 0;
        }

        public bool TryGetClientQuoteIndex(int clientIndex, int quoteId, out int quoteIndex, bool logErrorIfNotFound = true)
        {
            quoteIndex = this.UserClients[clientIndex].quotes.FindIndex(
                x => x.id == quoteId
            );

            if (quoteIndex < 0 && logErrorIfNotFound)
            {
                MelcherLog.LogError($"Did not find quote with ID {quoteId} in client #{this.UserClients[clientIndex].id}");
            }

            return quoteIndex >= 0;
        }

        public string TryGetClientCombinedName()
        {
            string clientName = "Guest";
            if (this.ActiveClientId.HasValue &&
                this.TryGetClientIndex(this.ActiveClientId.Value, out int clientIndex))
            {
                UserClient client = this.UserClients[clientIndex];

                string[] names = new[] { client.firstName, client.lastName };

                clientName = string.Join(" ", names);
            }

            return clientName;
        }
    }
}
