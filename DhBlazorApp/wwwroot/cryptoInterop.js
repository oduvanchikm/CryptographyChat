export async function generatePublicKey() {
    return await DotNet.invokeMethodAsync('DhBlazorApp', 'GeneratePublicKey');
}

export async function computeSharedSecret(otherPublicKeyStr) {
    return await DotNet.invokeMethodAsync('DhBlazorApp', 'ComputeSharedSecret', otherPublicKeyStr);
}
