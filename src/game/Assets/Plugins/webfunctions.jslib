var plugin = {
  $constants: {
    taintRocketLeaderboardEnabledStorageKey: "TAINTROCKET_LEADERBOARD_ENABLED",
    taintRocketSessionIdStorageKey: "TAINTROCKET_SESSION_ID",
    isGuid: function (value) {
      return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.exec(
        value
      );
    },
  },

  RedirectToItchAuthorizationPage: function (
    leaderboardBaseUrl,
    sessionSecret
  ) {
    window.open(
      `${UTF8ToString(leaderboardBaseUrl)}/login#session_secret=${UTF8ToString(
        sessionSecret
      )}`,
      "_blank"
    );
  },

  GetLeaderboardsDisabled: function () {
    return (
      localStorage.getItem(
        constants.taintRocketLeaderboardEnabledStorageKey
      ) === "0"
    );
  },

  GetLeaderboardsEnabled: function () {
    return (
      localStorage.getItem(
        constants.taintRocketLeaderboardEnabledStorageKey
      ) === "1"
    );
  },

  PersistLeaderboardsEnabled: function (enabled) {
    localStorage.setItem(
      constants.taintRocketLeaderboardEnabledStorageKey,
      enabled.toString()
    );
  },

  UnsetPersistedLeaderboardsEnabled: function () {
    localStorage.removeItem(constants.taintRocketLeaderboardEnabledStorageKey);
  },

  GetLeaderboardSessionGuid: function () {
    const storedValue = localStorage.getItem(
      constants.taintRocketSessionIdStorageKey
    );
    if (storedValue && constants.isGuid(storedValue)) {
      var buffer = _malloc(lengthBytesUTF8(storedValue) + 1);
      writeStringToMemory(storedValue, buffer);
      return buffer;
    }

    return null;
  },

  PersistLeaderboardSessionGuid: function (guid) {
    localStorage.setItem(
      constants.taintRocketSessionIdStorageKey,
      UTF8ToString(guid)
    );
  },

  SyncIndexedDB: function () {
    FS.syncfs(false, function (err) {});
  },
};

autoAddDeps(plugin, "$constants");
mergeInto(LibraryManager.library, plugin);
