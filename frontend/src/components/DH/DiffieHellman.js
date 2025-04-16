/* global BigInt */

class DiffieHellman {
    constructor(p, g) {
        console.debug('start constructor')
        this.P = BigInt('0x' + String(p).trim());
        console.debug('start constructor 1')
        this.G = BigInt(String(g).trim());
        console.debug('start constructor 2')
        this.privateKey = null;
        console.debug('start constructor 3')
        this.publicKey = null;
        console.debug('start constructor 4')

        console.debug('end constructor')
        this.generateKeys();
        console.debug('after generate keys constructor')
    }

    modPow(base, exponent, modulus) {
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

    generateKeys() {
        console.debug('start generateKeys()')
        this.privateKey = this.generatePrivateKey();
        console.debug('2 generateKeys')
        this.publicKey = this.modPow(this.G, this.privateKey, this.P);
        console.debug('3 generateKeys')
    }

    generatePrivateKey() {
        console.debug('start generatePrivateKey()')
        const array = new Uint8Array(32);
        console.debug('2 generatePrivateKey')
        crypto.getRandomValues(array);
        console.debug('3 generatePrivateKey')
        let hex = '0x' + Array.from(array, x => x.toString(16).padStart(2, '0')).join('');
        console.debug('4 generatePrivateKey')
        const privateKey = BigInt(hex);
        console.debug('5 generatePrivateKey')
        return privateKey % (this.P - 1n) + 1n;
    }

    computeSharedSecret(otherPublicKey) {
        console.debug('1 computeSharedSecret')
        if (!otherPublicKey) {
            throw new Error('Public key is required');
        }
        console.debug('2 computeSharedSecret')

        try {
            console.debug('3 computeSharedSecret')
            const key = typeof otherPublicKey === 'string'
                ? otherPublicKey.trim()
                : String(otherPublicKey);
            console.debug('4 computeSharedSecret')

            if (!/^\d+$/.test(key)) {
                throw new Error('Invalid public key format');
            }
            console.debug('5 computeSharedSecret')

            return this.modPow(BigInt(key), this.privateKey, this.P);
        } catch (e) {
            console.error('Error computing shared secret:', e);
            throw new Error('Failed to compute shared secret');
        }
    }
}

export default DiffieHellman;