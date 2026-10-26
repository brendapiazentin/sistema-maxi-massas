import 'package:flutter/material.dart';

import '../../core/tema.dart';

/// Provisória: o histórico real de pedidos entra na etapa seguinte.
class PedidosScreen extends StatelessWidget {
  const PedidosScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        backgroundColor: Tema.vinho,
        foregroundColor: Colors.white,
        surfaceTintColor: Colors.transparent,
        title: const Text('Meus pedidos'),
      ),
      body: const Center(
        child: Padding(
          padding: EdgeInsets.all(32),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(Icons.receipt_long_outlined, size: 56, color: Colors.black38),
              SizedBox(height: 12),
              Text('Nenhum pedido ainda', style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600)),
              SizedBox(height: 4),
              Text('Quando você fizer um pedido, ele aparece aqui.',
                  textAlign: TextAlign.center, style: TextStyle(color: Colors.black54)),
            ],
          ),
        ),
      ),
    );
  }
}
