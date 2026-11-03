import 'package:flutter/services.dart';

String soDigitos(String s) => s.replaceAll(RegExp(r'\D'), '');

/// Máscara de telefone brasileiro: (16) 99999-9999 ou (16) 9999-9999.
class MascaraTelefone extends TextInputFormatter {
  @override
  TextEditingValue formatEditUpdate(TextEditingValue antigo, TextEditingValue novo) {
    var d = soDigitos(novo.text);
    if (d.length > 11) d = d.substring(0, 11);
    final b = StringBuffer();
    for (var i = 0; i < d.length; i++) {
      if (i == 0) b.write('(');
      if (i == 2) b.write(') ');
      if (d.length > 10 ? i == 7 : i == 6) b.write('-');
      b.write(d[i]);
    }
    final t = b.toString();
    return TextEditingValue(text: t, selection: TextSelection.collapsed(offset: t.length));
  }
}
