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

  RedirectToItchAuthorizationPage: function (leaderboardBaseUrl) {
    location.replace(`${Pointer_stringify(leaderboardBaseUrl)}/login`);
  },

  GetLeaderboardsDisabled: function () {
    return (
      localStorage.getItem(
        constants.taintRocketLeaderboardEnabledStorageKey
      ) === "false"
    );
  },

  PersistLeaderboardsEnabled: function (enabled) {
    localStorage.setItem(
      constants.taintRocketLeaderboardEnabledStorageKey,
      enabled.toString()
    );
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
      Pointer_stringify(guid)
    );
  },

  ExtractSessionGuidFromFragment: function () {
    const parts = location.hash.split("=");
    window.location.replace("#");

    if (
      parts.length === 2 &&
      parts[0] === "#session_secret" &&
      constants.isGuid(parts[1])
    ) {
      var buffer = _malloc(lengthBytesUTF8(parts[1]) + 1);
      writeStringToMemory(parts[1], buffer);
      return buffer;
    }

    return null;
  },
  
  SyncIndexedDB: function() {
    FS.syncfs(false, function (err) {});
  },
};

autoAddDeps(plugin, "$constants");
mergeInto(LibraryManager.library, plugin);
