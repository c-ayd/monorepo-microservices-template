from cryptography.hazmat.primitives.asymmetric import rsa
from cryptography.hazmat.backends import default_backend
from cryptography.hazmat.primitives import serialization
from pathlib import Path
import os

def generate_rsa_keys():
    private_key = rsa.generate_private_key(
        backend=default_backend(),
        public_exponent=65537,
        key_size=2048
    )
    private_pem = private_key.private_bytes(
        encoding=serialization.Encoding.PEM,
        format=serialization.PrivateFormat.PKCS8,
        encryption_algorithm=serialization.NoEncryption()
    )

    public_key = private_key.public_key()
    public_pem = public_key.public_bytes(
        encoding=serialization.Encoding.PEM,
        format=serialization.PublicFormat.SubjectPublicKeyInfo
    )

    private_pem_oneline = private_pem.decode('utf-8').replace('\n', '')
    public_pem_oneline = public_pem.decode('utf-8').replace('\n', '')

    folder_path = Path(__file__).resolve().parent / 'output'
    if not os.path.exists(folder_path):
        os.makedirs(folder_path)

    with open(f'{folder_path}/rsa_private.pem', 'wb') as file:
        file.write(private_pem)
    with open(f'{folder_path}/rsa_public.pem', 'wb') as file:
        file.write(public_pem)

    with open(folder_path / 'rsa_private_oneline.txt', 'w') as file:
        file.write(private_pem_oneline)
    with open(folder_path / 'rsa_public_oneline.txt', 'w') as file:
        file.write(public_pem_oneline)

    print(f'RSA keys are created at {folder_path}')

if __name__ == '__main__':
    generate_rsa_keys()
