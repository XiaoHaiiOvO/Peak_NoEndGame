# Campfire Respawn

[English](#english) · [简体中文](#简体中文)

---

# English

## About

Campfire Respawn prevents a PEAK run from ending immediately when the entire party dies or becomes fully passed out. Instead, the party is revived at the current area's safe respawn point so the run can continue.

Before reviving the party, the mod resets advancing hazards such as fog, lava, and Gloom. It can then restore either the inventory recorded at the latest campfire or the items dropped during the wipe.

Current version: `2.0.0`

## Features

### Continue after a party wipe

When every player is dead or fully passed out, the host automatically starts a campfire respawn:

1. The normal game-over sequence is temporarily blocked.
2. Fog, lava, Gloom, and other active rising hazards are reset.
3. Dead and fully passed-out players are revived.
4. The party is moved to the appropriate reconnect, campfire, or special spawn point.
5. Player items are restored according to the selected inventory mode.
6. One available respawn is consumed.

Normal areas, The Kiln, PEAK, and Void use their own appropriate spawn rules.

When no respawns remain, or when the current map has no valid respawn point, the game is allowed to proceed to its normal results screen.

### Hazard reset

Before each respawn, the mod resets the current area's advancing hazard:

- Fog is moved back to the current area's initial radius.
- Lava, Gloom, or another rising field is returned to its starting position.
- Hazards are not disabled permanently. They can begin advancing again according to the normal game rules.

### Two inventory recovery modes

The `recordItemsAtCampfire` setting controls how items are restored.

#### Campfire checkpoint mode

Setting: `recordItemsAtCampfire = true`

This is the default mode.

- An initial inventory checkpoint is recorded after players leave the starting beach.
- The party inventory checkpoint is updated whenever a progression campfire is lit.
- After a wipe, each revived player receives the items they had at the latest checkpoint.
- All three normal inventory slots and the separate backpack slot are included.
- Durability, remaining uses, and other supported per-item data are copied from the checkpoint.
- Duplicate death drops are removed before checkpoint items are restored.

Use this mode when you want a wipe to behave like returning to the latest campfire save point.

#### Death-drop recovery mode

Setting: `recordItemsAtCampfire = false`

- Campfire inventory checkpoints are not used.
- After the party is revived, the mod attempts to return the items dropped during that wipe to their original owners.
- Items that fail the recovery chance are not returned to the inventory and remain where they were dropped.

Use this mode when you only want the mod to pick up the items lost in the latest wipe.

### Remaining-respawn display

At the beginning of a run and after each respawn, the bottom of the screen briefly displays:

`♥ Remaining respawns`

The display hides automatically after about 10 seconds. In multiplayer, the number is controlled by the host.

### Manual host respawn

The host can press the configured hotkey to force a party respawn. The default key is `F11`.

A manual respawn will:

- Revive dead or fully passed-out players.
- Move players who are still alive to the same respawn point.
- Clear normally curable negative statuses from surviving players.
- Reset the current area's advancing hazard.
- Consume one available respawn.

Pressing the hotkey as a non-host client will not trigger a party respawn.

### Campfire status clearing

When `CampfireClearStatus` is enabled, standing within the effective radius of a lit campfire automatically clears negative statuses that the current game considers curable.

The following status types are not directly removed by this feature:

- Curse
- Petrify
- Weight
- Crab
- Thorns
- Arrow

This is a local setting and can be configured separately by each player.

## Installation

A working BepInEx 5 installation for PEAK is required.

1. Close the game.
2. Open the PEAK installation directory.
3. Open `BepInEx\plugins`.
4. Optionally create a folder named `CampfireRespawn`.
5. Copy `Peak_NoEndGame.dll` to:

```text
PEAK\BepInEx\plugins\CampfireRespawn\Peak_NoEndGame.dll
```

6. Start the game once.
7. BepInEx will create the configuration file at:

```text
PEAK\BepInEx\config\com.Xiaohai.CampfireRespawn.cfg
```

When upgrading from an older version, back up and remove the old DLL first. Do not keep multiple versions in different plugin folders.

## Multiplayer requirements

- Every player should install the same mod version.
- Using the same configuration is strongly recommended, especially for `recordItemsAtCampfire`.
- Automatic respawning, the maximum respawn count, revive penalties, and item recovery chance are controlled by the current host.
- Campfire status clearing is controlled by each player's local configuration.
- Normal host migration is supported. Clients already in the room keep their own campfire inventory checkpoint.
- A client without the mod may not see the remaining-respawn UI and may log missing custom RPC messages.

## Configuration reference

Close the game before editing the configuration file.

### `CampfireClearStatus`

Default: `false`

- `true`: Periodically clears curable negative statuses while the player is close to a lit campfire.
- `false`: Keeps the game's normal campfire status behavior.

### `ReviveClearStatus`

Default: `false`

This legacy setting name is preserved for compatibility with older configuration files. Its actual meaning is whether the normal game revive penalties should be applied.

- `true`: Apply the game's normal Curse and Hunger penalties after revival.
- `false`: Clear statuses during revival without adding Curse or Hunger penalties.

### `RespawnHotkey`

Default: `F11`

Sets the hotkey the host can use to force a party respawn.

### `RespawnMaxTimes`

Default: `99`

Sets the maximum number of respawns allowed in one run.

- `0`: Disable party-wipe respawning and use the normal game-over flow.
- Allowed range: `0` to `999`.

### `recordItemsAtCampfire`

Default: `true`

- `true`: Restore items from the latest campfire inventory checkpoint.
- `false`: Recover the items actually dropped during the latest wipe.

After changing this setting, start a new run instead of switching modes in the middle of a run.

### `RespawnItemChance`

Default: `88`

The recovery chance is rolled separately for every item.

- `100`: Every recoverable item is restored.
- `50`: Each item has a 50% chance to be restored.
- `0`: No items are returned automatically.
- Allowed range: `0` to `100`.

## Recommended configurations

### Light recovery

```ini
CampfireClearStatus = false
ReviveClearStatus = true
RespawnMaxTimes = 3
recordItemsAtCampfire = false
RespawnItemChance = 75
```

Keeps the normal revive penalties and returns only some death drops. Recommended for groups that want limited protection without greatly reducing the difficulty.

### Campfire save-point mode

```ini
CampfireClearStatus = false
ReviveClearStatus = false
RespawnMaxTimes = 10
recordItemsAtCampfire = true
RespawnItemChance = 100
```

Returns the party to the latest campfire inventory state. Suitable for casual play or route practice.

### Practice-oriented mode

```ini
CampfireClearStatus = true
ReviveClearStatus = false
RespawnMaxTimes = 999
recordItemsAtCampfire = true
RespawnItemChance = 100
```

Suitable for map exploration and route practice, but it substantially reduces the normal difficulty.

## First-time test

Test the mod in a private lobby after installation:

1. Confirm that every player has installed the same version.
2. Light a campfire that advances the current area.
3. Note the items carried by each player.
4. Let the entire party die or become fully passed out.
5. Confirm that the party respawns in the correct area.
6. Confirm that fog or lava has returned to its starting position.
7. Check that the inventory result matches the selected recovery mode and chance.
8. Check that the remaining-respawn count decreases.
9. Test both `recordItemsAtCampfire = true` and `false` at least once.

## Troubleshooting

### Nothing happens in game

Check that:

- BepInEx is installed and starts correctly.
- The DLL is inside `BepInEx\plugins` or one of its subfolders.
- Multiple old versions of the DLL are not installed at the same time.
- `BepInEx\LogOutput.log` contains:

```text
Campfire Respawn v2.0.0 loaded
```

### The run still ends after a party wipe

Possible causes:

- `RespawnMaxTimes` is `0`, or all available respawns have been consumed.
- The current player is not the host and the actual host does not have the mod installed.
- The current map has not created a valid respawn point.
- A Harmony patch failed while the mod was loading.

Check `BepInEx\LogOutput.log` for errors containing `Campfire Respawn`, `Harmony`, or `respawn`.

### Not every item is restored

Check `RespawnItemChance`. The chance is rolled separately for each item, and the default value of `88` does not guarantee full restoration.

To always restore eligible items, use:

```ini
RespawnItemChance = 100
```

In death-drop recovery mode, items that are not restored remain at their drop location.

### Restored items do not match the inventory at death

This is expected when `recordItemsAtCampfire = true`. This mode restores the latest campfire checkpoint, not the inventory from the moment of death.

To recover the items dropped at death, use:

```ini
recordItemsAtCampfire = false
```

### Clients show different counts or RPC errors

Confirm that every player is using version `2.0.0` and remove any older DLLs. Mixing versions can cause the parameters of custom multiplayer messages to differ.

### F11 does not work

- Only the host can force a party respawn.
- The party must already be in an active run.
- At least one respawn must remain.
- A short cooldown prevents the hotkey from triggering repeatedly.
- Check whether `RespawnHotkey` has been changed to another key.

## Uninstallation

1. Close the game.
2. Delete `BepInEx\plugins\CampfireRespawn\Peak_NoEndGame.dll`.
3. To remove the saved settings as well, delete:

```text
BepInEx\config\com.Xiaohai.CampfireRespawn.cfg
```

The default configuration will be generated again if the mod is reinstalled later.

---

# 简体中文

## MOD 简介

Campfire Respawn 可以在《PEAK》中阻止全队团灭后立即结束本局，并将队伍重新送回当前区域的安全复活点。

复活时，MOD 会重置正在逼近的雾、岩浆或其他上升危险，并根据配置恢复玩家物品。它适用于单人游戏和多人联机，实际复活流程由房主控制。

当前版本：`2.0.0`

## 主要功能

### 全队团灭后继续游戏

当所有玩家都已经死亡或完全昏迷时，房主会自动触发篝火复活：

1. 暂停正常的游戏结束流程。
2. 重置当前区域的雾、岩浆或 Gloom 等上升危险。
3. 将死亡或完全昏迷的玩家复活。
4. 将队伍传送到当前区域对应的重连点、篝火或特殊出生点。
5. 按配置恢复玩家物品。
6. 消耗一次可用复活次数。

普通区域、The Kiln、PEAK 和 Void 都有独立的复活点处理。

当复活次数用完，或当前地图没有有效复活点时，MOD 会放行游戏原本的结算流程。

### 危险区域重置

每次复活前会重置当前区域正在推进的危险：

- 将迷雾退回当前区域的初始范围。
- 将当前区域的岩浆、Gloom 或其他上升场重新置于起点。
- 危险机制不会被永久关闭，复活后仍会按照游戏规则重新开始推进。

### 两种物品恢复方式

MOD 提供两种物品处理模式，由 `recordItemsAtCampfire` 控制。

#### 篝火快照模式

设置：`recordItemsAtCampfire = true`

这是默认模式。

- 玩家离开初始海滩时会记录一次背包。
- 每次点亮用于推进区域的篝火时，会更新全队物品记录。
- 团灭复活后，恢复最后一次记录时玩家拥有的物品。
- 三个普通物品栏和独立背包栏都会记录。
- 物品的耐久、剩余次数及其他实例数据会尽量保持记录时的状态。
- 团灭时产生的重复掉落物会被清理，避免同时获得记录物品和死亡掉落物。

这种模式适合希望“回到最近篝火存档状态”的玩家。

#### 死亡掉落找回模式

设置：`recordItemsAtCampfire = false`

- 不使用篝火物品快照。
- 玩家复活后，MOD 会尝试把这次死亡实际掉落的物品重新放回对应玩家背包。
- 未通过恢复概率的物品不会放回背包，会继续留在掉落位置。

这种模式更接近“把刚刚掉落的装备捡回来”。

### 剩余次数显示

本局开始和每次复活后，画面下方会短暂显示：

`♥剩余复活次数`

显示约 10 秒后自动隐藏。多人游戏中的数值以房主为准。

### 房主强制复活

房主可以按下配置的快捷键手动触发一次复活，默认是 `F11`。

强制复活会：

- 复活死亡或完全昏迷的玩家。
- 将仍然存活的玩家一起传送到复活点。
- 清除存活玩家可正常治愈的负面状态。
- 重置当前区域危险。
- 消耗一次复活次数。

普通客户端按快捷键不会触发全队复活。

### 篝火状态清除

启用 `CampfireClearStatus` 后，玩家站在已点燃篝火的有效范围内时，会自动清除游戏判定为可治愈的负面状态。

以下类型不会被该功能直接清除：

- Curse
- Petrify
- Weight
- Crab
- Thorns
- Arrow

该设置由每名玩家自己的本地配置决定。

## 安装方法

安装前需要先为《PEAK》安装可用的 BepInEx 5。

1. 关闭游戏。
2. 打开《PEAK》的游戏目录。
3. 进入 `BepInEx\plugins`。
4. 建议新建 `CampfireRespawn` 文件夹。
5. 将 `Peak_NoEndGame.dll` 放入：

```text
PEAK\BepInEx\plugins\CampfireRespawn\Peak_NoEndGame.dll
```

6. 启动一次游戏。
7. BepInEx 会自动生成配置文件：

```text
PEAK\BepInEx\config\com.Xiaohai.CampfireRespawn.cfg
```

如果正在升级旧版本，请先备份并移除旧 DLL，避免不同目录里同时存在多个版本。

## 多人联机要求

- 建议房间内所有玩家都安装相同版本的 MOD。
- 建议所有玩家使用相同配置，尤其是 `recordItemsAtCampfire`。
- 自动复活、复活次数、复活惩罚和物品恢复概率由当前房主的配置决定。
- 篝火状态清除属于本地功能，由每名玩家自己的配置决定。
- 正常的房主迁移受到支持；已经在场的客户端会各自保存篝火物品快照。
- 未安装 MOD 的客户端可能看不到剩余次数 UI，并可能在日志中出现找不到自定义 RPC 的提示。

## 配置项详解

修改配置前建议先关闭游戏。

### `CampfireClearStatus`

默认值：`false`

- `true`：站在已点燃篝火附近时，定期清除可治愈负面状态。
- `false`：保持游戏原本的篝火状态处理。

### `ReviveClearStatus`

默认值：`false`

这是为了兼容旧版本配置而保留的历史名称，实际含义是“是否使用游戏原生复活惩罚”。

- `true`：复活后施加游戏原生的 Curse 和 Hunger 惩罚。
- `false`：复活时清除状态，但不追加 Curse 和 Hunger 惩罚。

### `RespawnHotkey`

默认值：`F11`

设置房主强制触发全队复活的快捷键。

### `RespawnMaxTimes`

默认值：`99`

设置每局最多允许复活多少次。

- `0`：禁用团灭复活，游戏使用正常结算流程。
- 可设置范围：`0` 至 `999`。

### `recordItemsAtCampfire`

默认值：`true`

- `true`：恢复最后一次篝火记录的物品。
- `false`：找回本次死亡实际掉落的物品。

改变该设置后，建议重新开始一局游戏，不要在同一局中途切换。

### `RespawnItemChance`

默认值：`88`

每一件物品单独进行恢复判定。

- `100`：所有可恢复物品都会恢复。
- `50`：每件物品有 50% 概率恢复。
- `0`：物品不会被自动放回背包。
- 可设置范围：`0` 至 `100`。

## 推荐配置

### 轻度容错

```ini
CampfireClearStatus = false
ReviveClearStatus = true
RespawnMaxTimes = 3
recordItemsAtCampfire = false
RespawnItemChance = 75
```

保留复活惩罚，只找回部分死亡掉落物，适合不希望明显降低难度的队伍。

### 篝火存档模式

```ini
CampfireClearStatus = false
ReviveClearStatus = false
RespawnMaxTimes = 10
recordItemsAtCampfire = true
RespawnItemChance = 100
```

团灭后完整回到最近篝火的物品状态，适合休闲或练习路线。

### 无限练习倾向

```ini
CampfireClearStatus = true
ReviveClearStatus = false
RespawnMaxTimes = 999
recordItemsAtCampfire = true
RespawnItemChance = 100
```

适合地图探索和路线练习，但会大幅降低正常游戏难度。

## 建议测试方法

第一次安装后，建议在私人房间进行一次测试：

1. 确认所有玩家安装相同版本。
2. 点亮一个能够推进区域的篝火。
3. 记住每名玩家当时的物品。
4. 让全队死亡或完全昏迷。
5. 确认队伍在正确区域复活。
6. 确认雾或岩浆已经退回起点。
7. 检查物品结果是否符合当前恢复模式和概率。
8. 检查剩余复活次数是否减少。
9. 分别测试一次 `recordItemsAtCampfire = true` 和 `false`。

## 常见问题

### 游戏中完全没有反应

检查：

- BepInEx 是否正确安装并正常启动。
- DLL 是否位于 `BepInEx\plugins` 或其子目录。
- 是否同时残留了多个旧版本 DLL。
- `BepInEx\LogOutput.log` 中是否出现：

```text
Campfire Respawn v2.0.0 loaded
```

### 团灭后仍然直接结束游戏

可能原因：

- `RespawnMaxTimes` 设置为 `0`，或者本局次数已经用完。
- 当前玩家不是房主，而房主没有安装 MOD。
- 当前地图尚未生成有效复活点。
- MOD 在加载时发生 Harmony 补丁错误。

请查看 `BepInEx\LogOutput.log` 中包含 `Campfire Respawn`、`Harmony` 或 `respawn` 的错误信息。

### 物品没有全部恢复

检查 `RespawnItemChance`。该概率会对每件物品分别判定，默认 `88` 并不保证全部恢复。

如果希望完整恢复，请设置：

```ini
RespawnItemChance = 100
```

在死亡掉落找回模式下，未成功恢复的物品会留在原掉落位置。

### 恢复的物品不是最近死亡时携带的物品

如果 `recordItemsAtCampfire = true`，这是正常行为。该模式恢复的是最近一次篝火快照，而不是死亡瞬间的背包。

如果需要恢复死亡时掉落的物品，请改为：

```ini
recordItemsAtCampfire = false
```

### 客户端次数显示不一致或出现 RPC 错误

确认所有玩家都使用 `2.0.0`，并删除旧版 DLL。不同版本混用时，自定义联机消息的参数可能不一致。

### F11 没有效果

- 只有房主可以触发强制复活。
- 必须已经进入正式登山流程。
- 当前复活次数不能用完。
- 两次复活之间有短暂冷却，避免重复触发。
- 检查 `RespawnHotkey` 是否已经改为其他按键。

## 卸载方法

1. 关闭游戏。
2. 删除 `BepInEx\plugins\CampfireRespawn\Peak_NoEndGame.dll`。
3. 如果还需要移除个人设置，请删除：

```text
BepInEx\config\com.Xiaohai.CampfireRespawn.cfg
```

以后重新安装 MOD 时，会再次生成默认配置。
