const crypto = require('crypto');
const { publicKey, privateKey } = crypto.generateKeyPairSync('ec', { namedCurve: 'P-256' });
const pub = publicKey.export({ format: 'jwk' });
const pri = privateKey.export({ format: 'jwk' });
const diem = Buffer.concat([Buffer.from([4]), Buffer.from(pub.x, 'base64url'), Buffer.from(pub.y, 'base64url')]);
console.log('VAPID_PUBLIC (dán vào push.js và wrangler.toml):');
console.log(diem.toString('base64url'));
console.log('\nVAPID_PRIVATE_JWK (dán làm secret, không chia sẻ):');
console.log(JSON.stringify({ kty: 'EC', crv: 'P-256', x: pub.x, y: pub.y, d: pri.d }));