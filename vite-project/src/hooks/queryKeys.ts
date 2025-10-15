export const PREFS_QUERY_KEY         = ["preferences"] as const;
export const PREFS_DTO_QUERY_KEY     = ["prefs", "dto"] as const;
export const NEWS_FOR_ME_QUERY_KEY   = ["news", "for-me"] as const;

export const REACTIONS_QUERY_KEY = (ids: string[]) =>
  ["reactions", ...ids.slice().sort()] as const;
