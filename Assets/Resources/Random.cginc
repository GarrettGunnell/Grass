uint rng_state;

//Hash invented by Thomas Wang
void wang_hash(uint seed) {
    rng_state = (seed ^ 61) ^ (seed >> 16);
    rng_state *= 9;
    rng_state = rng_state ^ (rng_state >> 4);
    rng_state *= 0x27d4eb2d;
    rng_state = rng_state ^ (rng_state >> 15);
}

//Xorshift algorithm from George Marsaglia's paper
uint rand_xorshift() {
    rng_state ^= (rng_state << 13);
    rng_state ^= (rng_state >> 17);
    rng_state ^= (rng_state << 5);

    return rng_state;
}

float randValue() {
    return rand_xorshift() * (1.0 / 4294967296.0);
}

void initRand(uint seed) {
    wang_hash(seed);
}

float randValue(uint seed) {
    initRand(seed);
    return randValue();
}