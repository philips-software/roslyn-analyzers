# Security

The "Philips Security" category of diagnostics flags potential security issues.

## PH2158: Avoid PKCS#1 v1.5 padding with RSA encryption

**Summary:** RSA should use OAEP padding instead of PKCS#1 v1.5 padding for better security.

**Rationale:** PKCS#1 v1.5 padding is vulnerable to padding oracle attacks. OAEP (Optimal Asymmetric Encryption Padding) provides better security by using a more robust padding scheme that is resistant to such attacks.

**How to fix:** Replace `RSAEncryptionPadding.Pkcs1` with secure alternatives like:
- `RSAEncryptionPadding.OaepSHA1`
- `RSAEncryptionPadding.OaepSHA256`
- `RSAEncryptionPadding.OaepSHA384`
- `RSAEncryptionPadding.OaepSHA512`

**Example:**
```csharp
// Bad
byte[] encrypted = rsa.Encrypt(data, RSAEncryptionPadding.Pkcs1);

// Good
byte[] encrypted = rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
```

**More Information:** [How much safer is RSA OAEP compared to RSA with PKCS1 v1.5 padding?](https://crypto.stackexchange.com/questions/47436/how-much-safer-is-rsa-oaep-compared-to-rsa-with-pkcs1-v1-5-padding)
