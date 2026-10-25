import 'package:intl/intl.dart';

final NumberFormat _moeda = NumberFormat.currency(locale: 'pt_BR', symbol: 'R\$');

/// Formata um valor em reais, ex.: R$ 39,90.
String reais(num valor) => _moeda.format(valor);
