using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Peak_NoEndGame
{
    public sealed class ReviewHandler : MonoBehaviourPun
    {
        private const float RespawnCooldown = 5f;
        private const float WipeCheckInterval = 0.2f;
        private const float StatusClearInterval = 3f;

        private readonly InventoryCheckpoint _checkpoint = new InventoryCheckpoint();
        private bool _runActive;
        private bool _respawning;
        private bool _warnedMissingSpawn;
        private float _lastRespawnTime = float.NegativeInfinity;
        private float _nextWipeCheck;
        private float _nextStatusClear;
        private float _fogResetSize = 300f;

        public static ReviewHandler Instance { get; private set; }
        public int reviveTimes { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                ReviewUI.Hide();
            }
        }

        private void Update()
        {
            if (!_runActive || Plugin.Instance == null)
            {
                return;
            }

            TryClearLocalCampfireStatuses();
            TryCaptureInitialCheckpoint();

            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }

            if (Input.GetKeyUp(Plugin.Instance.RespawnHotkey.Value))
            {
                TryStartRespawn(true);
            }

            if (Time.unscaledTime < _nextWipeCheck)
            {
                return;
            }

            _nextWipeCheck = Time.unscaledTime + WipeCheckInterval;
            if (AllPlayersDeadOrDown())
            {
                TryStartRespawn(false);
            }
        }

        internal void BeginRun()
        {
            _runActive = true;
            _respawning = false;
            _warnedMissingSpawn = false;
            _lastRespawnTime = float.NegativeInfinity;
            _nextWipeCheck = 0f;
            _nextStatusClear = 0f;
            reviveTimes = 0;
            _checkpoint.Clear();
            ReviewUI.Show(Plugin.Instance == null ? 0 : Plugin.Instance.MaximumRespawns);
            if (Plugin.Instance != null && PhotonNetwork.IsMasterClient)
            {
                SetReviveTimesNetworked(0);
            }
        }

        internal void EndRun()
        {
            _runActive = false;
            _respawning = false;
            _checkpoint.Clear();
            ReviewUI.Hide();
        }

        internal void RememberFogOrigin(FogSphereOrigin origin)
        {
            if (origin != null)
            {
                _fogResetSize = origin.size;
            }
        }

        internal void CaptureCampfireCheckpoint()
        {
            if (!_runActive || Plugin.Instance == null || !Plugin.Instance.RecordItemsAtCampfire.Value)
            {
                return;
            }

            CaptureCheckpoint("campfire");
        }

        internal bool ShouldSuppressEndGame()
        {
            if (!_runActive || Plugin.Instance == null || !PhotonNetwork.IsMasterClient ||
                reviveTimes >= Plugin.Instance.MaximumRespawns || !AllPlayersDead())
            {
                return false;
            }

            Vector3 spawnPosition;
            if (!TryGetRespawnPosition(out spawnPosition))
            {
                WarnMissingSpawnOnce();
                return false;
            }

            // If a wipe reaches Character.CheckEndGame during the cooldown, suppress the
            // end screen; Update will start the queued revive as soon as the cooldown ends.
            if (!_respawning && Time.time - _lastRespawnTime >= RespawnCooldown)
            {
                TryStartRespawn(false);
            }

            return true;
        }

        private void TryCaptureInitialCheckpoint()
        {
            if (_checkpoint.IsCaptured || Plugin.Instance == null ||
                !Plugin.Instance.RecordItemsAtCampfire.Value || !MapHandler.ExistsAndInitialized ||
                MapHandler.CurrentSegmentNumber != Segment.Beach)
            {
                return;
            }

            Character local = Character.localCharacter;
            Vector3 respawnPosition;
            if (local == null || !TryGetRespawnPosition(out respawnPosition) ||
                Vector3.Distance(local.Center, respawnPosition) <= 120f)
            {
                return;
            }

            CaptureCheckpoint("shore departure");
            ShowTitleNetworked("已记录玩家物品!", "Items Recorded");
        }

        private void CaptureCheckpoint(string reason)
        {
            int count = _checkpoint.Capture(Character.AllCharacters.ToArray());
            Plugin.Log.LogInfo(string.Format("Inventory checkpoint captured ({0}), {1} item(s).", reason, count));
        }

        private bool TryStartRespawn(bool forced)
        {
            if (!_runActive || _respawning || Plugin.Instance == null || !PhotonNetwork.IsMasterClient ||
                reviveTimes >= Plugin.Instance.MaximumRespawns || Time.time - _lastRespawnTime < RespawnCooldown)
            {
                return false;
            }

            Vector3 spawnPosition;
            if (!TryGetRespawnPosition(out spawnPosition))
            {
                WarnMissingSpawnOnce();
                return false;
            }

            if (!forced && !AllPlayersDeadOrDown())
            {
                return false;
            }

            _warnedMissingSpawn = false;
            _respawning = true;
            _lastRespawnTime = Time.time;
            StartCoroutine(RespawnRoutine(spawnPosition));
            return true;
        }

        private IEnumerator RespawnRoutine(Vector3 spawnPosition)
        {
            IEnumerator routine = RespawnRoutineCore(spawnPosition);
            try
            {
                while (true)
                {
                    object current;
                    try
                    {
                        if (!routine.MoveNext())
                        {
                            break;
                        }

                        current = routine.Current;
                    }
                    catch (Exception exception)
                    {
                        Plugin.Log.LogError("Respawn routine failed: " + exception);
                        break;
                    }

                    yield return current;
                }
            }
            finally
            {
                _respawning = false;
            }
        }

        private IEnumerator RespawnRoutineCore(Vector3 spawnPosition)
        {
            List<Character> characters = Character.AllCharacters.Where(character => character != null).ToList();
            HashSet<int> revivedActors = new HashSet<int>();
            Peak.VoidBiome voidBiome = MapHandler.CurrentSegmentNumber == Segment.Void ? Peak.VoidBiome.instance : null;
            int voidSpawnIndex = 0;

            IEnumerator resetHazards = ResetHazards();
            while (resetHazards.MoveNext())
            {
                yield return resetHazards.Current;
            }

            foreach (Character character in characters)
            {
                if (character == null || character.data == null || character.photonView == null)
                {
                    continue;
                }

                Vector3 target = voidBiome != null
                    ? voidBiome.GetSpawnPosition(voidSpawnIndex++) + Vector3.up
                    : spawnPosition + new Vector3(Random.Range(-3f, 3f), 1f, Random.Range(-3f, 3f));
                if (character.data.dead || character.data.fullyPassedOut)
                {
                    int actorNumber;
                    if (InventoryCheckpoint.TryGetActorNumber(character, out actorNumber))
                    {
                        revivedActors.Add(actorNumber);
                    }

                    character.photonView.RPC(
                        "RPCA_ReviveAtPosition",
                        RpcTarget.All,
                        target,
                        Plugin.Instance.ReviveAddCurse.Value,
                        -1);
                }
                else
                {
                    character.photonView.RPC("WarpPlayerRPC", RpcTarget.All, target, true);
                    photonView.RPC(nameof(RPCA_ClearLocalStatuses), RpcTarget.All, character.photonView.ViewID);
                }
            }

            Plugin.Log.LogInfo(string.Format("Respawning party at {0}; affected players: {1}.", spawnPosition, revivedActors.Count));
            ShowTitleNetworked("继续加油", "Nice Try");
            SetReviveTimesNetworked(reviveTimes + 1);

            float deadline = Time.realtimeSinceStartup + 5f;
            while ((AnyActorStillDeadOrDown(revivedActors) || AnyDroppableItemsRemain(revivedActors)) &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            if (AnyDroppableItemsRemain(revivedActors))
            {
                Plugin.Log.LogWarning("Timed out waiting for one or more players to finish dropping their inventory.");
            }

            // Remote owners send their DropAllItems RPCs from RPCA_ReviveAtPosition.
            // One more frame lets the master's dropped-item cache settle before restore.
            yield return null;

            if (Plugin.Instance.RecordItemsAtCampfire.Value && _checkpoint.IsCaptured)
            {
                CleanupDroppedItems(revivedActors);
                yield return null;
                RestoreCheckpointItems(revivedActors);
            }
            else
            {
                RestoreDroppedItems(revivedActors);
            }
        }

        private IEnumerator ResetHazards()
        {
            OrbFogHandler fog = UnityEngine.Object.FindObjectOfType<OrbFogHandler>();
            PhotonView fogView = fog == null ? null : fog.GetComponent<PhotonView>();
            if (fog != null && fogView != null)
            {
                fog.currentSize = _fogResetSize;
                fogView.RPC("RPCA_SyncFog", RpcTarget.All, _fogResetSize, fog.isMoving);
            }

            Segment currentSegment = MapHandler.Exists ? MapHandler.CurrentSegmentNumber : Segment.Beach;
            List<LavaRising> lavaFields = LavaRising.ALL_LAVA
                .Where(lava => lava != null && lava.requiredSegment == currentSegment && lava.photonView != null)
                .ToList();

            foreach (LavaRising lava in lavaFields)
            {
                lava.photonView.RPC("RPC_SyncLava", RpcTarget.All, true, false, 0f, 0f);
            }

            if (lavaFields.Count > 0)
            {
                yield return null;
                foreach (LavaRising lava in lavaFields)
                {
                    if (lava != null && lava.photonView != null)
                    {
                        lava.photonView.RPC("RPC_SyncLava", RpcTarget.All, false, false, 0f, 0f);
                    }
                }
            }
        }

        private void RestoreCheckpointItems(IEnumerable<int> actorNumbers)
        {
            int restored = 0;
            foreach (int actorNumber in actorNumbers)
            {
                Character character = FindCharacter(actorNumber);
                if (character == null || character.player == null)
                {
                    continue;
                }

                List<RecordedInventoryItem> missingItems = new List<RecordedInventoryItem>(_checkpoint.GetItems(actorNumber));
                RemoveItemsAlreadyCarried(character.player, missingItems);
                foreach (RecordedInventoryItem recordedItem in missingItems)
                {
                    if (!RollItemRestore())
                    {
                        continue;
                    }

                    ItemSlot restoredSlot;
                    ItemInstanceData data = recordedItem.Data == null ? null : recordedItem.Data.Copy();
                    if (data != null)
                    {
                        ItemInstanceDataHandler.AddInstanceData(data);
                    }

                    if (character.player.AddItem(recordedItem.ItemId, data, out restoredSlot))
                    {
                        restored++;
                    }
                    else
                    {
                        Plugin.Log.LogWarning("Could not restore item " + recordedItem.ItemName + " to " + character.characterName + ".");
                    }
                }

                character.refs.items.RefreshAllCharacterCarryWeight();
            }

            Plugin.Log.LogInfo("Restored " + restored + " checkpoint item(s).");
        }

        private static void RemoveItemsAlreadyCarried(Player player, IList<RecordedInventoryItem> missingItems)
        {
            for (byte slotId = 0; slotId <= 3; slotId++)
            {
                ItemSlot slot = player.GetItemSlot(slotId);
                if (slot == null || slot.IsEmpty() || slot.prefab == null)
                {
                    continue;
                }

                for (int itemIndex = 0; itemIndex < missingItems.Count; itemIndex++)
                {
                    if (missingItems[itemIndex].ItemId == slot.prefab.itemID)
                    {
                        missingItems.RemoveAt(itemIndex);
                        break;
                    }
                }
            }
        }

        private void RestoreDroppedItems(IEnumerable<int> actorNumbers)
        {
            int restored = 0;
            foreach (int actorNumber in actorNumbers)
            {
                Character character = FindCharacter(actorNumber);
                if (character == null || character.refs == null || character.refs.items == null)
                {
                    continue;
                }

                List<PhotonView> droppedViews = character.refs.items.droppedItems.ToList();
                character.refs.items.droppedItems.Clear();
                foreach (PhotonView droppedView in droppedViews)
                {
                    if (droppedView == null || !RollItemRestore())
                    {
                        continue;
                    }

                    Item item = droppedView.GetComponent<Item>();
                    if (item != null)
                    {
                        item.RequestPickup(character.photonView);
                        restored++;
                    }
                }
            }

            Plugin.Log.LogInfo("Recovered " + restored + " dropped item(s).");
        }

        private static void CleanupDroppedItems(IEnumerable<int> actorNumbers)
        {
            foreach (int actorNumber in actorNumbers)
            {
                Character character = FindCharacter(actorNumber);
                if (character == null || character.refs == null || character.refs.items == null)
                {
                    continue;
                }

                List<PhotonView> droppedViews = character.refs.items.droppedItems.ToList();
                character.refs.items.droppedItems.Clear();
                foreach (PhotonView droppedView in droppedViews)
                {
                    if (droppedView != null)
                    {
                        PhotonNetwork.Destroy(droppedView.gameObject);
                    }
                }
            }
        }

        private bool RollItemRestore()
        {
            return Random.Range(0, 100) < Plugin.Instance.ItemRestoreChance;
        }

        private void TryClearLocalCampfireStatuses()
        {
            if (!Plugin.Instance.CampfireClearStatus.Value || Time.unscaledTime < _nextStatusClear ||
                !MapHandler.ExistsAndInitialized)
            {
                return;
            }

            _nextStatusClear = Time.unscaledTime + StatusClearInterval;
            Character local = Character.localCharacter;
            if (local == null || local.data == null || local.data.dead || local.refs == null ||
                local.refs.afflictions == null || local.refs.afflictions.currentStatuses == null)
            {
                return;
            }

            Segment currentSegment = MapHandler.CurrentSegmentNumber;
            int currentSegmentIndex = currentSegment == Segment.Void ? -1 : (int)currentSegment;
            Campfire campfire = GetCampfire(currentSegmentIndex);
            if (!IsInsideCampfire(local, campfire))
            {
                campfire = GetCampfire(currentSegmentIndex - 1);
            }

            if (!IsInsideCampfire(local, campfire))
            {
                return;
            }

            foreach (CharacterAfflictions.STATUSTYPE status in Enum.GetValues(typeof(CharacterAfflictions.STATUSTYPE)))
            {
                int index = (int)status;
                if (ShouldClearAtCampfire(status) && index >= 0 && index < local.refs.afflictions.currentStatuses.Length &&
                    local.refs.afflictions.currentStatuses[index] > 0f)
                {
                    local.refs.afflictions.ClearAllStatus(true, true);
                    break;
                }
            }
        }

        private static bool ShouldClearAtCampfire(CharacterAfflictions.STATUSTYPE status)
        {
            switch (status)
            {
                case CharacterAfflictions.STATUSTYPE.Weight:
                case CharacterAfflictions.STATUSTYPE.Crab:
                case CharacterAfflictions.STATUSTYPE.Thorns:
                case CharacterAfflictions.STATUSTYPE.Curse:
                case CharacterAfflictions.STATUSTYPE.Arrow:
                case CharacterAfflictions.STATUSTYPE.Petrify:
                    return false;
                default:
                    return true;
            }
        }

        private static bool IsInsideCampfire(Character character, Campfire campfire)
        {
            return campfire != null && campfire.Lit &&
                   Vector3.Distance(character.Center, campfire.transform.position) <= campfire.moraleBoostRadius;
        }

        private static Campfire GetCampfire(int segmentIndex)
        {
            GameObject root = segmentIndex < 0 ? null : MapHandler.GetCampfireRoot(segmentIndex);
            return root == null ? null : root.GetComponentInChildren<Campfire>(true);
        }

        private static bool TryGetRespawnPosition(out Vector3 position)
        {
            position = default(Vector3);
            if (!MapHandler.ExistsAndInitialized)
            {
                return false;
            }

            Segment currentSegment = MapHandler.CurrentSegmentNumber;
            if (currentSegment == Segment.Void)
            {
                Peak.VoidBiome voidBiome = Peak.VoidBiome.instance;
                if (voidBiome != null && voidBiome.spawnPoints != null && voidBiome.spawnPoints.Length > 0)
                {
                    position = voidBiome.GetSpawnPosition(0);
                    return true;
                }

                return false;
            }

            MapHandler map = UnityEngine.Object.FindObjectOfType<MapHandler>();
            if (currentSegment == Segment.Peak && map != null && map.respawnThePeak != null)
            {
                position = map.respawnThePeak.position;
                return true;
            }

            MapHandler.MapSegment segment = MapHandler.CurrentMapSegment;
            if (segment != null && segment.reconnectSpawnPos != null)
            {
                position = segment.reconnectSpawnPos.position;
                return true;
            }

            Transform baseCampSpawn = MapHandler.CurrentBaseCampSpawnPoint;
            if (baseCampSpawn != null)
            {
                position = baseCampSpawn.position;
                return true;
            }

            if (map != null && map.respawnThePeak != null)
            {
                position = map.respawnThePeak.position;
                return true;
            }

            return false;
        }

        private static Character FindCharacter(int actorNumber)
        {
            foreach (Character character in Character.AllCharacters)
            {
                int candidateActor;
                if (InventoryCheckpoint.TryGetActorNumber(character, out candidateActor) && candidateActor == actorNumber)
                {
                    return character;
                }
            }

            return null;
        }

        private static bool AllPlayersDeadOrDown()
        {
            bool foundCharacter = false;
            foreach (Character character in Character.AllCharacters)
            {
                if (character == null || character.data == null)
                {
                    continue;
                }

                foundCharacter = true;
                if (!character.data.dead && !character.data.fullyPassedOut)
                {
                    return false;
                }
            }

            return foundCharacter;
        }

        private static bool AllPlayersDead()
        {
            bool foundCharacter = false;
            foreach (Character character in Character.AllCharacters)
            {
                if (character == null || character.data == null)
                {
                    continue;
                }

                foundCharacter = true;
                if (!character.data.dead)
                {
                    return false;
                }
            }

            return foundCharacter;
        }

        private static bool AnyActorStillDeadOrDown(IEnumerable<int> actorNumbers)
        {
            foreach (int actorNumber in actorNumbers)
            {
                Character character = FindCharacter(actorNumber);
                if (character != null && character.data != null && (character.data.dead || character.data.fullyPassedOut))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AnyDroppableItemsRemain(IEnumerable<int> actorNumbers)
        {
            foreach (int actorNumber in actorNumbers)
            {
                Character character = FindCharacter(actorNumber);
                if (character == null || character.player == null)
                {
                    continue;
                }

                for (byte slotId = 0; slotId <= 3; slotId++)
                {
                    ItemSlot slot = character.player.GetItemSlot(slotId);
                    if (slot != null && !slot.IsEmpty() && slot.prefab != null && slot.prefab.UIData.canDrop)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void SetReviveTimesNetworked(int times)
        {
            int remaining = Mathf.Max(0, Plugin.Instance.MaximumRespawns - times);
            photonView.RPC(nameof(RPCA_SetReviveTimes), RpcTarget.All, times, remaining);
        }

        private void ShowTitleNetworked(string chinese, string english)
        {
            photonView.RPC(nameof(RPCA_ShowTitle), RpcTarget.All, chinese, english);
        }

        private void WarnMissingSpawnOnce()
        {
            if (_warnedMissingSpawn)
            {
                return;
            }

            _warnedMissingSpawn = true;
            Plugin.Log.LogWarning("No valid campfire respawn point is available; allowing the normal game-over flow.");
        }

        [PunRPC]
        public void RPCA_SetReviveTimes(int times, int remaining)
        {
            reviveTimes = times;
            ReviewUI.Show(Mathf.Max(0, remaining));
        }

        [PunRPC]
        public void RPCA_ShowTitle(string chinese, string english)
        {
            if (GUIManager.instance == null)
            {
                return;
            }

            AudioClip clip = null;
            MountainProgressHandler progress = UnityEngine.Object.FindObjectOfType<MountainProgressHandler>();
            if (progress != null && progress.progressPoints != null && progress.progressPoints.Length > 0)
            {
                int index = Mathf.Clamp(progress.maxProgressPointReached, 0, progress.progressPoints.Length - 1);
                clip = progress.progressPoints[index].clip;
            }

            string title = LocalizedText.CURRENT_LANGUAGE == LocalizedText.Language.SimplifiedChinese
                ? chinese
                : english;
            GUIManager.instance.SetHeroTitle(title, clip, false);
        }

        [PunRPC]
        public void RPCA_ClearLocalStatuses(int characterViewId)
        {
            PhotonView targetView = PhotonNetwork.GetPhotonView(characterViewId);
            Character character = targetView == null ? null : targetView.GetComponent<Character>();
            if (character != null && character.IsLocal && character.refs != null && character.refs.afflictions != null)
            {
                character.refs.afflictions.ClearAllStatus(true, true);
            }
        }
    }
}
