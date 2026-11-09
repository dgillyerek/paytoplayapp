// App Store (StoreKit 2) validation: STUB behind the same interface as Google Play, so iOS can be switched on later.
// To finish it: verify the client's signedTransaction (JWS) chain against Apple Root CA G3, or call the App Store
// Server API GET /inApps/v1/transactions/{transactionId} with an ES256 JWT built from APP_STORE_ISSUER_ID,
// APP_STORE_KEY_ID and APP_STORE_PRIVATE_KEY (.p8), then check bundleId === APP_STORE_BUNDLE_ID, productId, and
// revocationDate. Map the answer to the same { state, orderId, purchasedAt, raw } shape the Google verifier returns.
export class AppStoreNotEnabled extends Error {
  constructor() { super("App Store validation is not enabled yet"); this.code = "app_store_not_enabled"; }
}

export function appStoreClient(_env = {}) {
  return {
    enabled: false,
    async verifyTransaction(_signedTransaction, _transactionId) { throw new AppStoreNotEnabled(); },
  };
}
