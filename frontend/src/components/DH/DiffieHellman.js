/* global BigInt */
class DiffieHellman {
    constructor(bitLength = 128) {
        this.bitLength = bitLength;
        this.p = this.generateProbablePrime(bitLength);
        this.g = this.findPrimitiveRootSimple(this.p);
        this.privateKey = this.generatePrivateKey();
        this.publicKey = this.generatePublicKey();
    }

    generateRandomBigInt(bitLength) {
        const byteLength = Math.ceil(bitLength / 8);
        const randomBytes = new Uint8Array(byteLength);
        crypto.getRandomValues(randomBytes);

        randomBytes[0] |= 0x80;
        randomBytes[byteLength - 1] |= 0x01;

        let hexString = '0x';
        randomBytes.forEach(byte => {
            hexString += byte.toString(16).padStart(2, '0');
        });

        return BigInt(hexString);
    }

    generateProbablePrime(bitLength, certainty = 20) {
        let candidate;
        do {
            candidate = this.generateRandomBigInt(bitLength);
        } while (!this.isProbablePrime(candidate, certainty));

        return candidate;
    }

    isProbablePrime(n, k) {
        if (n <= 1n) return false;
        if (n <= 3n) return true;
        if (n % 2n === 0n) return false;

        let d = n - 1n;
        let s = 0n;
        while (d % 2n === 0n) {
            d /= 2n;
            s++;
        }

        for (let i = 0; i < k; i++) {
            const a = this.getRandomBigInt(2n, n - 2n);
            let x = this.modPow(a, d, n);

            if (x === 1n || x === n - 1n) continue;

            let j;
            for (j = 1n; j < s; j++) {
                x = this.modPow(x, 2n, n);
                if (x === n - 1n) break;
            }

            if (j === s) return false;
        }

        return true;
    }

    findPrimitiveRootSimple(p) {
        if (p === 2n) return 1n;
        const pMinus1 = p - 1n;

        const candidates = [2n, 3n, 5n, 7n, 11n];
        for (const g of candidates) {
            if (this.modPow(g, pMinus1, p) === 1n) {
                return g;
            }
        }

        throw new Error('Failed to find primitive root');
    }

    generatePrivateKey() {
        const min = 2n;
        const max = this.p - 2n;
        const range = max - min;
        const byteLength = Math.ceil(this.bitLength / 8);
        const randomBytes = new Uint8Array(byteLength);

        let privateKey;
        do {
            crypto.getRandomValues(randomBytes);
            let hexString = '0x';
            randomBytes.forEach(byte => {
                hexString += byte.toString(16).padStart(2, '0');
            });
            privateKey = BigInt(hexString) % range + min;
        } while (privateKey <= 1n || privateKey >= this.p - 1n);

        return privateKey;
    }

    generatePublicKey() {
        return this.modPow(this.g, this.privateKey, this.p);
    }

    computeSharedSecret(otherPublicKey) {
        return this.modPow(otherPublicKey, this.privateKey, this.p);
    }

    modPow(base, exponent, modulus) {
        if (modulus === 1n) return 0n;
        let result = 1n;
        base = base % modulus;

        while (exponent > 0n) {
            if (exponent % 2n === 1n) {
                result = (result * base) % modulus;
            }
            exponent = exponent >> 1n;
            base = (base * base) % modulus;
        }

        return result;
    }

    getRandomBigInt(min, max) {
        const range = max - min;
        const byteLength = Math.ceil(range.toString(2).length / 8);
        const randomBytes = new Uint8Array(byteLength);

        let result;
        do {
            crypto.getRandomValues(randomBytes);
            let hexString = '0x';
            randomBytes.forEach(byte => {
                hexString += byte.toString(16).padStart(2, '0');
            });
            result = BigInt(hexString) % range + min;
        } while (result < min || result > max);

        return result;
    }
}

export default DiffieHellman;