# MSIX 开发签名证书

## 用途与文件

Vence 的自签名开发证书用于 MSIX 安装包签名，主题为 `CN=Vence Development`，与应用清单的 Publisher 一致。

- [Vence.TestCertificate.cer](Vence.TestCertificate.cer) 是公开证书，供测试电脑导入并信任。
- `artifacts/certs/Vence.TestCertificate.pfx` 包含签名私钥，由本地备份和 GitHub Actions Secrets 保存。
- Git 忽略规则排除 PFX、P12 私钥文件及生成的安装包。

安装包签名用于验证发布者及包内容的完整性。自签名证书适用于开发和测试，测试电脑需要信任相应证书。

## GitHub Actions Secrets

仓库使用以下 Secrets 保存开发签名资料：

| Secret | 内容 |
| --- | --- |
| `VENCE_TEST_CERTIFICATE_PFX_BASE64` | PFX 文件的 Base64 编码 |
| `VENCE_TEST_CERTIFICATE_PASSWORD` | PFX 密码 |

CI 在运行时解码 PFX 至临时目录，从 Secrets 读取密码，并传入 `scripts/package-msix.ps1` 的 `CertificatePath` 和 `CertificatePassword` 参数。Base64 用于表示二进制内容；Secrets 负责保存敏感值。

本地备份继续保留原始 PFX，供本地打包和恢复使用。

## 重新生成

现有脚本支持重新生成开发证书：

```powershell
.\scripts\package-msix.ps1 -ForceNewCertificate
```

此命令会生成新的证书和私钥，覆盖本地证书文件，并生成签名安装包。生成前请保留旧证书备份。使用新证书签名的安装包，测试电脑需要导入并信任新的 CER；CI Secrets 和仓库中的公开 CER 也应同步更新。

## 参考

- [微软：创建安装包签名证书](https://learn.microsoft.com/en-us/windows/msix/package/create-certificate-package-signing)
- [GitHub：在 Actions 中使用 Secrets](https://docs.github.com/en/actions/how-tos/write-workflows/choose-what-workflows-do/use-secrets)
