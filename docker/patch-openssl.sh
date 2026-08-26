#!/bin/sh
set -eu
CNF=/etc/ssl/openssl.cnf
cp "$CNF" "$CNF.bak"

if ! grep -q '^ssl_conf' "$CNF"; then
  sed -i '/^\[openssl_init\]/a ssl_conf = nile_ssl_conf' "$CNF"
fi

if grep -q '^# *legacy = legacy_sect' "$CNF"; then
  sed -i 's/^# *legacy = legacy_sect/legacy = legacy_sect/' "$CNF"
fi

if ! grep -q '^\[legacy_sect\]' "$CNF"; then
  printf '\n[legacy_sect]\nactivate = 1\n' >> "$CNF"
fi

if ! grep -q '^\[nile_ssl_defaults\]' "$CNF"; then
  cat >> "$CNF" <<'EOF'

[nile_ssl_conf]
system_default = nile_ssl_defaults

[nile_ssl_defaults]
MinProtocol = TLSv1
MaxProtocol = TLSv1.2
CipherString = DEFAULT:@SECLEVEL=0
Options = UnsafeLegacyServerConnect,UnsafeLegacyRenegotiation
EOF
fi
